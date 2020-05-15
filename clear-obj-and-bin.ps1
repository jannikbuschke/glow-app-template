Remove-Item .\.vs -Recurse -Force
Get-ChildItem .\ -include bin,obj,node_modules,.vs -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }
