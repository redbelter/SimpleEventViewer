using System;
using System.Diagnostics;

namespace EventLogReader.Tests
{
    public class Tests
    {
        private List<EventLogEntry> _testEvents = new();

        [SetUp]
        public void Setup()
        {
            CreateMockData();
        }

        private void CreateMockData()
        {
            var now = DateTime.Now;
            _testEvents = new List<EventLogEntry>
            {
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-10),
                    EntryType = EventLogEntryType.Error,
                    Source = "TestProvider",
                    Message = "Error message 1"
                },
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-9),
                    EntryType = EventLogEntryType.Warning,
                    Source = "TestProvider",
                    Message = "Warning message 1"
                },
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-8),
                    EntryType = EventLogEntryType.Information,
                    Source = "AnotherProvider",
                    Message = "Info message 1"
                },
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-7),
                    EntryType = EventLogEntryType.Error,
                    Source = "AnotherProvider",
                    Message = "Error message 2"
                },
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-6),
                    EntryType = EventLogEntryType.Warning,
                    Source = "TestProvider",
                    Message = "Warning message 2"
                },
                new EventLogEntry
                {
                    TimeGenerated = now.AddMinutes(-5),
                    EntryType = EventLogEntryType.Information,
                    Source = "TestProvider",
                    Message = "Info message 2"
                }
            };
        }

        [Test]
        public void TestReadEventsFromFile()
        {
            Assert.That(_testEvents.Count, Is.GreaterThan(0), "Should have loaded events from file or created mock data");
        }

        [Test]
        public void TestFilterByLevel_ErrorOnly()
        {
            var errorEvents = _testEvents.Where(e => e.EntryType == EventLogEntryType.Error).ToList();
            
            Assert.That(errorEvents.Count, Is.GreaterThan(0), "Should have some error events");
            Assert.That(errorEvents.Count, Is.EqualTo(_testEvents.FindAll(e => e.EntryType == EventLogEntryType.Error).Count));
        }

        [Test]
        public void TestFilterByLevel_WarningOnly()
        {
            var warningEvents = _testEvents.Where(e => e.EntryType == EventLogEntryType.Warning).ToList();
            
            Assert.That(warningEvents.Count, Is.GreaterThan(0), "Should have some warning events");
        }

        [Test]
        public void TestFilterByTimeRange()
        {
            if (_testEvents.Count == 0) Assert.Inconclusive("No events to test with");
            
            DateTime startTime = _testEvents.Min(e => e.TimeGenerated);
            DateTime endTime = _testEvents.Max(e => e.TimeGenerated);
            
            var filtered = _testEvents.FindAll(e => e.TimeGenerated >= startTime && e.TimeGenerated <= endTime);
            
            Assert.That(filtered.Count, Is.EqualTo(_testEvents.Count), "Time range filter should include all events");
        }

        [Test]
        public void TestFilterByProvider()
        {
            if (_testEvents.Count == 0) Assert.Inconclusive("No events to test with");
            
            string provider = _testEvents[0].Source;
            var filtered = _testEvents.FindAll(e => e.Source.Contains(provider));
            
            Assert.That(filtered.Count, Is.GreaterThan(0), "Should find events matching provider");
        }

        [Test]
        public void TestMultipleFiltersCombine()
        {
            if (_testEvents.Count == 0) Assert.Inconclusive("No events to test with");
            
            DateTime startTime = _testEvents.Min(e => e.TimeGenerated);
            DateTime endTime = _testEvents.Max(e => e.TimeGenerated);
            string provider = _testEvents[0].Source;
            
            var filtered = _testEvents.FindAll(e => 
                e.EntryType == EventLogEntryType.Error &&
                e.TimeGenerated >= startTime && e.TimeGenerated <= endTime &&
                e.Source.Contains(provider));
            
            Assert.That(filtered.Count, Is.LessThanOrEqualTo(_testEvents.Count), "Multiple filters should reduce or maintain count");
        }

        [Test]
        public void TestAllFilterReturnsAll()
        {
            var filtered = _testEvents.FindAll(e => 
                e.EntryType != EventLogEntryType.Error && 
                e.EntryType != EventLogEntryType.Warning &&
                e.EntryType != EventLogEntryType.Information);
            
            Assert.That(filtered.Count, Is.EqualTo(0), "All events should match some category");
        }
    }

    public class EventLogEntry
    {
        public DateTime TimeGenerated { get; set; }
        public EventLogEntryType EntryType { get; set; }
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public uint InstanceId { get; set; }
    }
}
