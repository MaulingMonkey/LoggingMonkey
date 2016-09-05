function status
{
	$statusUrl	= 'http://test.maulingmonkey.com/build/status.txt'
	$branch		= $env:CI_BUILD_REF_NAME
	$commit		= $env:CI_BUILD_REF.Substring(0,6)
	$stage		= $env:CI_BUILD_STAGE
	$job		= $env:CI_BUILD_NAME
	$message	= $args[0]
	$fullStatus	= "$branch ($commit..) | $stage | $job | $message"

	# only echo the message - other info should be obvious from gitlab CI context
	echo $message

	# use the entire fullStatus - MMTest has no other context
	# Try { & curl -X PUT -d $fullStatus $statusUrl } Catch {}
	# Try { Invoke-RestMethod $statusUrl -Method Put -Body $fullStatus } Catch {}
}

function warning
{
	status "Warning: $args[0]"
}

function error
{
	status "Error: $args[0]"
	Exit 1
}
