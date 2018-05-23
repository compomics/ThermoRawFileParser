using System;
using System.Collections.Generic;
using System.Linq;
using IO.MzML;
using MassSpectrometry;

namespace ThermoRawFileParserTest
{
    public class FakeMsDataFile : MsDataFile<IMzmlScan>, IMsStaticDataFile<IMzmlScan>
    {
        #region Public Constructors

        public FakeMsDataFile(IMzmlScan[] FakeScans) : base(FakeScans,
            new SourceFile(@"scan number only nativeID format", "mzML format", null, "SHA-1", @"C:\fake.mzML", null))
        {
            this.Scans = FakeScans;
        }

        #endregion Public Constructors

        #region Public Methods

        public override int GetClosestOneBasedSpectrumNumber(double retentionTime)
        {
            int ok = Array.BinarySearch(Scans.Select(b => b.RetentionTime).ToArray(), retentionTime);
            if (ok < 0)
                ok = ~ok;
            return ok + 1;
        }

        public override IEnumerable<IMzmlScan> GetMS1Scans()
        {
            throw new NotImplementedException();
        }

        public override IMzmlScan GetOneBasedScan(int scanNumber)
        {
            return Scans[scanNumber - 1];
        }

        #endregion Public Methods
    }
}