# Potential Issues

## 1. Issue: NuGet Packages Not Available

**Symptom:** When trying to install packages like `EPPlus`, `MySql.Data`, or `Spectre.Console`, Visual Studio displays the message:

> *"Not available in this source."*

**Cause:** The only configured package source is offline:
> `Microsoft Visual Studio Offline Packages`

This source does not contain these packages.

**Solution:** Add the official `nuget.org` source:
1. Open Visual Studio.
2. Go to **Tools** > **Options** > **NuGet Package Manager** > **Package Sources**.
3. Click the **+** button.
4. Enter the following:
   - **Name:** `nuget.org`
   - **Source:** <https://api.nuget.org/v3/index.json>
5. Click **Update** then **OK**.
6. Select `nuget.org` as the package source in the package manager.


## 2. Issue: Autoconnect Doesn't Work

**Symptom:** When typing autoconnect this error shows : 

> *ERROR: Connection failed: Authentication to host 'localhost' for user 'root' using method 'caching_sha2_password' failed with message: Access denied for user 'root'@'localhost' (using password: YES)*
> *ERROR: MySQL connection failed.*

**Cause:** The autoconnect command start a new connection to the database without asking the password, by default it will be ```root```.

**Solution:** Change the default password :
1. Open the class **Commands** > **AutoConnectCommand.cs**
2. Go to line 46 and change the value of 'PASSWORD'