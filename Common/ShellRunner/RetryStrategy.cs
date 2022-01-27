namespace Common
{
    // TODO (DonMorozov): реализация выбора стратегии через enum - не самый красивый путь, для более красивого решения потребуются относительно масштабные изменения
    public enum RetryStrategy
    {
        None,
        IfTimeout,
        IfTimeoutOrFailed
    }
}