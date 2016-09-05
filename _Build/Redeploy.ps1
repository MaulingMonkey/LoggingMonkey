# Run specifically on test.maulingmonkey.com

. .\_Build\Common.ps1

$CONFIG	= $env:CONFIG
$PLATFORM	= $env:PLATFORM
$bin		= 'I:\home\bin\LoggingMonkey'


status 'Redeploying HTTP server'
#sleep -s 4 # Wait for updated status.txt to be served to anybody watching before shutting down

# Shut down existing HTTP server, if running, so the executable can be updated.
& taskkill /IM LoggingMonkey.exe | Out-Null
sleep -s 4 # Wait for taskkill to kill
& taskkill /F /IM LoggingMonkey.exe | Out-Null

# Remove legacy cruft
& rmdir /Q /S $bin

# Prepare directory structure
mkdir -Force $bin
& robocopy "bin\$CONFIG" $bin /S /NJH /NJS /NP

# Restart webserver
Start-Process $bin\LoggingMonkey.exe
if (!$?) { error 'Failed to restart webserver!' }

status 'Deployed'
