# ..\rhino\build\bin\nuget.exe install rhinocommon -verbosity normal
# ..\rhino\build\bin\nuget.exe install rhinocommon -prerelease -verbosity normal

src\WhatProp\bin\Release\WhatProp.exe (get-item RhinoCommon.6.*\lib\net45\RhinoCommon.dll).FullName > rhinocommon_6.txt
src\WhatProp\bin\Release\WhatProp.exe (get-item RhinoCommon.7.*\lib\net45\RhinoCommon.dll).FullName > rhinocommon_7.txt

# C:\Progra~1\Git\bin\bash -c "dos2unix rhinocommon_6.txt"
# C:\Progra~1\Git\bin\bash -c "dos2unix rhinocommon_7.txt"

C:\Progra~1\Git\bin\bash -c "unix2dos rhinocommon_6.txt"
C:\Progra~1\Git\bin\bash -c "unix2dos rhinocommon_7.txt"

# C:\Progra~1\Git\bin\bash -c "diff -U0 rhinocommon_6.txt rhinocommon_7.txt > rhinocommon_67.diff"

$cwd = $pwd.Path.split('\')[-1]
cd ..
$stat = & git diff --numstat $cwd\rhinocommon_6.txt $cwd\rhinocommon_7.txt
$stat
& git diff $cwd\rhinocommon_6.txt $cwd\rhinocommon_7.txt > $cwd\rhinocommon_67.diff
cd $cwd

$a,$b,$c = $stat.split()

$c = (Get-Content rhinocommon_7.txt | Measure-Object -Line).Lines

$churn = ([int]$a+[int]$b)/[int]$c*100 

$msg = '{0:0.##}% churn' -f $churn

if ($b -gt 0) {
    $msg += ' ({0} breaking changes)' -f $b
}

write-host $msg
write-host "##teamcity[buildStatus text='{build.status.text}, $msg']"
