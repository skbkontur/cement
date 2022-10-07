using JetBrains.Annotations;

namespace Common;

[PublicAPI]
public interface IGitRepositoryFactory
{
    GitRepository Create(string moduleName, string workspace);
    GitRepository Create(string repoPath);
}
