param (
    [parameter(Mandatory=$true)][string]$path
)



# config it

# $RHINO_OLD = "5.13.60523.20140"
# $RHINO_NEW = "6.0.16118.23591"
# $OUT_DIR = $env:TEMP + '\whatprop'
$OUT_DIR = 'out'

md -Force $OUT_DIR | Out-Null

$OUT_DIR
# $RHINO_OLD_DIR = $TMP_DIR + '\' + $RHINO_OLD
# $RHINO_NEW_DIR = $TMP_DIR + '\' + $RHINO_NEW
# $RHINO_OLD_FILE = $OUT_DIR + '\5.txt'
$RHINO_OLD_FILE = 'out\5.txt'
$RHINO_NEW_FILE = $OUT_DIR + '\wip.txt'
# $RHINO_NEW_FILE
# $RHINO_OLD_DLL = 'C:\Users\Will\Desktop\' + $RHINO_NEW + '\RhinoCommon.dll'

cmd /c src\WhatProp\bin\Debug\WhatProp.exe $path `> $RHINO_NEW_FILE

$cwd = $pwd.Path.split('\')[-1]
$RHINO_OLD_FILE = $cwd + '\' + $RHINO_OLD_FILE
$RHINO_NEW_FILE = $cwd + '\' + $RHINO_NEW_FILE

$AHA_FILE = $OUT_DIR + '\diff.html'

cd ..
$stat = & git diff --numstat $RHINO_OLD_FILE $RHINO_NEW_FILE
cmd /c git diff --color=always $RHINO_OLD_FILE $RHINO_NEW_FILE `| $cwd\aha.exe -s `> $cwd\$AHA_FILE
# cd whatprop

$a,$b,$c = $stat.split()
# $a
# $b
$c = Get-Content $RHINO_NEW_FILE | Measure-Object -Line
$c = $c.Lines

$churn = ([int]$a+[int]$b)/[int]$c*100 | % { '{0:0.##}% churn' -f $_ }
write-host $churn
write-host "##teamcity[buildStatus text='{build.status.text}, $churn']"

cd $cwd
