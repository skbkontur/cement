using System;

namespace Common.Updaters;

public interface ICementUpdater : IDisposable
{
    string Name { get; }

    string GetNewCommitHash();
    byte[] GetNewCementZip();
}
