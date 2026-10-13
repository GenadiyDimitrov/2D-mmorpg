# Hosting the server for friends (docs/guides/Hosting.md)

`BL-345`. Your question: *can the server run on an Oracle Always Free Linux machine?* **Yes.** Below is where it runs,
what it costs, the setup step by step, how friends get in, and the code that has to change first (part 4: none of it is
built yet).

---

## 1. The short answer

| | |
|---|---|
| **Where** | An Oracle Cloud **Always Free** ARM machine (Ampere A1), Ubuntu 24.04. |
| **Cost** | **€0 a month.** Oracle asks for a card at sign-up (identity check, not charged). The address is a free **DuckDNS** name. |
| **What friends do** | Open `https://<yourname>.duckdns.org` on the phone → tap *Download* → install → the login screen already holds the address → register → play. |
| **What you do per update** | `pwsh tools/publish.ps1 -Apk`, copy one zip to the box, run `./update.sh`. About two minutes. |
| **Before friends can join** | Six code changes (part 4). They are small, but two of them are security holes on a public server. |

### Why it fits
The server is plain .NET with SQLite: no Windows-only parts, no external database. The published build **already
contains the ARM Linux SQLite library** (`builds/Game.Server/runtimes/linux-arm64/`), and the server already runs on a
phone under Termux, which is a harsher place than a Linux VM. The game loop is one thread at 10 ticks a second; a
handful of players will not use a tenth of the machine.

### The limits, and the two catches
Oracle **halved** the free ARM allowance in mid-2026: it is now **2 OCPU and 12 GB of memory** in total (it was 4 / 24).
Machines larger than that were shut down in August 2026. One machine at **2 OCPU / 12 GB** is inside the new limit and
more than enough. Other free parts: 200 GB of disk, 10 TB of traffic a month.

1. **Idle reclamation.** Oracle may take back an Always Free machine that looks idle for 7 days: CPU under 20%, network
   under 20% and, on ARM, memory under 20%. **A small game server looks exactly like that.** The way out is to
   *upgrade the account to Pay-As-You-Go*. That stays €0 while you remain inside the free limits, and Oracle's rules
   exempt paid accounts from reclamation. Reports disagree on whether the halved limit also applies to paid accounts.
   At 2 OCPU / 12 GB you are inside both readings, so it does not matter. Set a **budget alert at €1** (step 1.4) so a
   mistake can never cost more than that.
2. **"Out of host capacity."** Free ARM machines are often sold out in popular regions. Your **home region is chosen
   once, at sign-up, and cannot be changed.** If creating the machine fails with that message, try again at another time
   of day, or pick a different *availability domain* on the same screen.

**Fallback if Oracle refuses you:** a Hetzner CAX11 (ARM, 2 cores / 4 GB, about €4-5 a month). The steps below are the
same from step 2 on.

---

## 2. Setup, step by step

Everything in a `grey box` is typed exactly as shown. `<yourname>` is your DuckDNS name; `<IP>` is the machine's public
address.

### Step 1: the Oracle account and the machine (once, ~30 min)
1. Sign up at **cloud.oracle.com** → *Start for free*. Pick a home region near you and your friends (permanent).
2. *Menu → Compute → Instances → Create instance.*
   - **Image:** *Change image* → **Canonical Ubuntu 24.04** (pick the one that is *not* "Minimal").
   - **Shape:** *Change shape* → *Ampere* → **VM.Standard.A1.Flex**, **2 OCPU, 12 GB**. It must show the *Always Free
     eligible* tag.
   - **SSH keys:** *Generate a key pair for me* → **Download private key**. Save it as `C:\Users\genad\.ssh\oracle.key`.
     Without this file you cannot get into the machine.
   - *Create*. After a minute it shows **Public IP address**. That is `<IP>`.
3. **Open the door in Oracle's firewall:** on the instance page → *Subnet* link → *Security Lists* → *Default Security
   List* → *Add Ingress Rules*. Source CIDR `0.0.0.0/0`, IP protocol TCP, destination port `80`. Add it, then add a
   second rule for port `443`. (Not 5238: the game port stays private, see step 4.)
4. *Billing → Upgrade and manage payment → Pay As You Go* (the reclamation catch above). Then *Billing → Budgets → Create
   budget*: amount **1**, alert at 100%, your e-mail.

### Step 2: the address (once, ~5 min)
1. Go to **duckdns.org**, sign in with Google.
2. Type a name (e.g. `emberfall`) → *add domain*. Put `<IP>` in the *current ip* box → *update ip*.
3. Your server's address is now `<yourname>.duckdns.org`. Oracle keeps the IP for as long as the machine exists.

### Step 3: get into the machine (each time you work on it)
In PowerShell on your PC:
```
ssh -i $HOME\.ssh\oracle.key ubuntu@<IP>
```
The first time it asks *"Are you sure…"*: type `yes`. You are now typing on the server. `exit` leaves.

### Step 4: prepare the machine (once, ~10 min, typed on the server)
```
sudo apt update && sudo apt -y upgrade
sudo apt -y install caddy sqlite3 unzip
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
mkdir -p ~/game ~/game/apk ~/backups
```
The two `iptables` lines matter: Oracle's Ubuntu has a **second firewall inside the machine** that blocks everything
except SSH. Forgetting it is the classic "I opened the port and it still doesn't connect".

**HTTPS (the padlock):** Caddy gets a free certificate by itself and passes the game traffic (WebSockets included)
through to the server. Its whole configuration is three lines:
```
sudo nano /etc/caddy/Caddyfile
```
Delete everything in it and type:
```
<yourname>.duckdns.org {
    reverse_proxy 127.0.0.1:5238
}
```
Ctrl+O, Enter, Ctrl+X to save, then `sudo systemctl reload caddy`.

### Step 5: the server as a service (once)
A *service* starts by itself when the machine boots and comes back if it crashes.
```
sudo nano /etc/systemd/system/game.service
```
```
[Unit]
Description=Game server
After=network.target

[Service]
User=ubuntu
WorkingDirectory=/home/ubuntu/game
ExecStart=/home/ubuntu/game/Game.Server
Environment=ASPNETCORE_URLS=http://127.0.0.1:5238
Environment=GAME_PUBLIC=1
Restart=always
RestartSec=5
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```
`127.0.0.1` means only Caddy can reach the game port; the world reaches it through HTTPS. `GAME_PUBLIC=1` switches off
the dev conveniences that are dangerous on a public server (part 4, items 1 and 2).
```
sudo systemctl daemon-reload
sudo systemctl enable game
```

### Step 6: the first install
On your PC:
```
pwsh tools/publish.ps1 -Apk -Linux
scp -i $HOME\.ssh\oracle.key builds\Game.Server-<version>-linux-arm64.zip builds\L2Clone-<version>.apk ubuntu@<IP>:~
```
On the server:
```
unzip -o ~/Game.Server-*-linux-arm64.zip -d ~/game && chmod +x ~/game/Game.Server
mv ~/L2Clone-*.apk ~/game/apk/
echo "<your character name>" > ~/game/owner.txt
sudo systemctl start game
journalctl -u game -f
```
The last line shows the server's console. Wait for `L2Clone server vX.Y.Z starting.`, then Ctrl+C (the server keeps
running). Open `https://<yourname>.duckdns.org` in the phone's browser: you should see *Game server vX.Y.Z*.

⚠ **Register YOUR account first** and create your character with the name you wrote in `owner.txt`. That makes you Owner.

### Step 7: updating to a new version (each release, ~2 min)
On the PC: `pwsh tools/publish.ps1 -Apk -Linux` and the same `scp` line. On the server, one command:
```
~/game/update.sh
```
It is shipped inside the zip (part 4, item 5). It stops the server, which saves everyone online, backs up `game.db`,
unpacks the new build over the old one, moves the APK into place and starts the server again. `game.db` and `owner.txt`
are never in the zip, so nothing of yours is overwritten.

⚠ **A version whose CHANGELOG says "delete `game.db`" wipes everyone's characters**, friends included. Pre-release that is
fine, but tell them first. The delete is `rm ~/game/game.db*` between stop and start.

### Step 8: backups (once)
```
crontab -e
```
Pick `nano` if it asks, and add this line at the bottom:
```
30 4 * * * sqlite3 /home/ubuntu/game/game.db ".backup /home/ubuntu/backups/game-$(date +\%F).db" && find /home/ubuntu/backups -name 'game-*.db' -mtime +14 -delete
```
Every night at 04:30 that makes a consistent copy (safe while the server runs) and keeps two weeks of them. They sit on
the same machine, which protects against mistakes but not against losing the machine. Once a week or so, pull the newest
one to your PC:
```
scp -i $HOME\.ssh\oracle.key "ubuntu@<IP>:~/backups/game-*.db" G:\Backups\game\
```
**To restore:** `sudo systemctl stop game`, `cp ~/backups/game-<date>.db ~/game/game.db`, `rm -f ~/game/game.db-wal
~/game/game.db-shm`, `sudo systemctl start game`.

---

## 3. How friends join

1. You send them one link: `https://<yourname>.duckdns.org`.
2. On the phone they tap **Download the Android client**. Android asks to *allow installing from this browser*; they say
   yes once, then *Install*.
3. The login screen already holds the server address (part 4, item 4). *Register* → pick a name and password → play.
4. **When you release a new version**, the server refuses the old APK with its version message. They open the same link
   and install over the top; their characters are on the server, not the phone.

Other ways to hand out the APK (a Discord message, a Google Drive link) work too; the server's own page is just the one
that is always up to date. Google Play needs a $25 developer account and a review, so it can wait for a public release.

---

## 4. What has to change in the code first (owed, not built)

| # | Change | Why | Side |
|---|---|---|---|
| 1 | **`owner.txt` is never auto-created when `GAME_PUBLIC=1`.** `ServerControl.EnsureOwnerFileForDev` writes a file naming **`Owner`**, so the first stranger to create a character called *Owner* would get the highest rank. Its own comment already says *"delete before going public"*; the env switch keeps it for your phone/PC loop. | security | server |
| 2 | **"The first account's first character is born Admin" is off when `GAME_PUBLIC=1`** (`PersistenceService.CreateCharacterAsync`). On a fresh public server, whoever registers first gets Admin. With `owner.txt` written by hand, you do not need it. | security | server |
| 3 | **A save on every stop, not only on `/shutdown`.** Today `AutoSaveAll` runs on the 60 s timer and on the admin countdown. `systemctl stop`, an update or an Oracle maintenance reboot loses up to a minute of everyone's progress. It should also save when the host asks the server to stop. A related fix: under a service, `/reboot` should only exit with its code and let systemd start it again. Today it launches a second copy itself, which systemd then kills. | data | server |
| 4 | **The login screen's default address becomes yours** (`GameBoot.ServerUrl` + the placeholder in `GameUi.cs`). The URL field stays, so you can still point at your PC or phone. HTTPS needs nothing else from the client. The SignalR client already speaks `https://`/`wss://`, but **this is untested on the phone**, so the first test is exactly that. The `usesCleartextTraffic` line stays for the LAN dev server. | friends | client → **new APK** |
| 5 | **`publish.ps1 -Linux`**: a *self-contained* `linux-arm64` build (`Game.Server-<ver>-linux-arm64.zip`), which carries its own .NET so the box needs no runtime installed and never gets one out of step. `update.sh` ships inside it. | ease | tools |
| 6 | **`/apk` serves the newest `apk/*.apk` next to the server.** Today it looks in `../Game.Client.Unity/builds/`, which exists only in the repo. | friends | server |

**Two smaller ones, worth doing in the same pass:**
- **Registration switch**: an Owner command `/registration open|closed`, remembered in a file. Open while your friends
  sign up, closed after, and strangers who find the address cannot make accounts. No client change.
- **The Android package id and signing key**, set once before friends install. The APK is still
  `com.UnityTechnologies.com.unity.template.urpblank` signed with your PC's debug key. A *later* change of either one
  means every friend must uninstall, which deletes nothing on the server but is a nuisance. The package id is invisible
  to players and has nothing to do with the game's final name, so a neutral one like `com.gdimitrov.mmo` is fine. The
  signing key is a file you keep backed up forever. Lose it and nobody can update; they can only reinstall.

**What is already fine and needs nothing:** passwords are stored salted and hashed (PBKDF2), not in plain text. HTTPS
protects them on the wire once Caddy is in front. Admin commands are gated by the character's role on the server. The
version check refuses stale clients. `game.db` runs in WAL mode, so the nightly `.backup` is safe while the server is up.

---

## 5. Order of work
1. The code changes in part 4 (one version; client change, so a new APK).
2. You do steps 1-8 above. I cannot do them for you: they need your Oracle login.
3. **First test, on your own phone over mobile data (Wi-Fi off):** connect to `https://<yourname>.duckdns.org/game`,
   register, play a minute, relog. Then send your friends the link.
