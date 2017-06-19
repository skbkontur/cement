function cm-ref-add {
    [CmdletBinding()]
    Param(
    )
 
    DynamicParam {
        # Set the dynamic parameters' name
        $ParameterName1 = 'Module'
            
        # Create the dictionary 
        $RuntimeParameterDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

        # Create the collection of attributes
        $AttributeCollection1 = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
            
        # Create and set the parameters' attributes
        $ParameterAttribute1 = New-Object System.Management.Automation.ParameterAttribute
        $ParameterAttribute1.Mandatory = $true
        $ParameterAttribute1.Position = 0

        # Add the attributes to the attributes collection
        $AttributeCollection1.Add($ParameterAttribute1)

        # Generate and set the ValidateSet
		$guid = "29593EF4-2FDF-4A3F-8CCF-B5ADECFFF55A"
		cm complete "cm ref add " > $guid
		$modules = Get-Content $guid
		del $guid
			 
        #$arrSet = Get-ChildItem -Path .\ -Directory | Select-Object -ExpandProperty FullName
        $ValidateSetAttribute1 = New-Object System.Management.Automation.ValidateSetAttribute($modules)

        # Add the ValidateSet to the attributes collection
        $AttributeCollection1.Add($ValidateSetAttribute1)

        # Create and return the dynamic parameter
        $RuntimeParameter1 = New-Object System.Management.Automation.RuntimeDefinedParameter($ParameterName1, [string], $AttributeCollection1)
        $RuntimeParameterDictionary.Add($ParameterName1, $RuntimeParameter1)

#############################

		# Set the dynamic parameters' name
        $ParameterName2 = 'Project'
            
        # Create the collection of attributes
        $AttributeCollection2 = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
            
        # Create and set the parameters' attributes
        $ParameterAttribute2 = New-Object System.Management.Automation.ParameterAttribute
        $ParameterAttribute2.Mandatory = $true
        $ParameterAttribute2.Position = 1

        # Add the attributes to the attributes collection
        $AttributeCollection2.Add($ParameterAttribute2)

        # Generate and set the ValidateSet
		# Generate and set the ValidateSet
		$guid = "29593EF4-2FDF-4A3F-8CCF-B5ADECFFF55B"
		
		cm complete "cm ref add * " > $guid
		$show = Get-Content $guid
		del $guid
			 
        #$arrSet = Get-ChildItem -Path .\ -Directory | Select-Object -ExpandProperty FullName
        $ValidateSetAttribute2 = New-Object System.Management.Automation.ValidateSetAttribute($show)

        # Add the ValidateSet to the attributes collection
        $AttributeCollection2.Add($ValidateSetAttribute2)

        # Create and return the dynamic parameter
        $RuntimeParameter2 = New-Object System.Management.Automation.RuntimeDefinedParameter($ParameterName2, [string], $AttributeCollection2)
        $RuntimeParameterDictionary.Add($ParameterName2, $RuntimeParameter2)

############################

		## Set the dynamic parameters' name
        $ParameterName3 = 'Configuration'
            
        # Create the collection of attributes
        $AttributeCollection3 = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
            
        # Create and set the parameters' attributes
        $ParameterAttribute3 = New-Object System.Management.Automation.ParameterAttribute
        $ParameterAttribute3.Mandatory = $false

        # Add the attributes to the attributes collection
        $AttributeCollection3.Add($ParameterAttribute3)

        # Create and return the dynamic parameter
        $RuntimeParameter3 = New-Object System.Management.Automation.RuntimeDefinedParameter($ParameterName3, [string], $AttributeCollection3)
        $RuntimeParameterDictionary.Add($ParameterName3, $RuntimeParameter3)

        return $RuntimeParameterDictionary
    }

    begin {
        # Bind the parameter to a friendly variable
        $Module = $PsBoundParameters[$ParameterName1]
        $Project = $PsBoundParameters[$ParameterName2]
        $Configuration = $PsBoundParameters[$ParameterName3]
    }

    process {		
		If ($Configuration.length -gt 0)
		{
			$Module = "$Module/$Configuration"
		}

		cm ref add $Module $Project --testReplaces
		If ($LASTEXITCODE -eq 0)
		{
			cm ref add $Module $Project
		}
		Else
		{
			start cm "ref add $Module $Project"
		}
    }
}