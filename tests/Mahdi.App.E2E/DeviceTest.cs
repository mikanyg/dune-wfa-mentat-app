namespace Mahdi.App.E2E;

/// <summary>
/// Base class running every test on both target devices in landscape.
/// </summary>
[TestFixture("iPad Pro 11 landscape")]
[TestFixture("iPhone 15 Pro landscape")]
[Parallelizable(ParallelScope.Self)]
public abstract class DeviceTest(string device) : AppTest(device);
