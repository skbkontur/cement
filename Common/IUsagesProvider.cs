namespace Common;

public interface IUsagesProvider
{
    ShowParentsAnswer GetUsages(string moduleName, string checkingBranch, string configuration = "*");
}
