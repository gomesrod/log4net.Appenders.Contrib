makecert.exe -r -pe -n "CN=localhost" -a sha1 -sky exchange -sv test.pvk test.cer
pvk2pfx -pvk test.pvk -spc test.cer -pfx test.pfx
