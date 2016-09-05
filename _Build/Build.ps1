# Run on many build machines

. .\_Build\Common.ps1

$CONFIG	= $env:CONFIG
$PLATFORM	= $env:PLATFORM

status 'Building...'
# TODO: Wrap in better MMTest build injestion tools
# Note that VS140 is VS 2015
& "$env:VS140COMNTOOLS\..\IDE\devenv.com" MMTest.sln /build "$CONFIG|$PLATFORM"
if (!$?) { error "Build FAILED" }
else { status 'Built OK' }
