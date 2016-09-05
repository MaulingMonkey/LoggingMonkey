# Run on many build machines

. .\_Build\Common.ps1

$CONFIG	= $env:CONFIG
$PLATFORM	= $env:PLATFORM

status 'Building...'
# TODO: Wrap in better build injestion tools
# Note that VS140 is VS 2015
& "$env:VS140COMNTOOLS\..\IDE\devenv.com" LoggingMonkey.sln /build "$CONFIG|$PLATFORM"
if (!$?) { error "Build FAILED" }
else { status 'Built OK' }
