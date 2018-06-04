using System;
using System.Collections.Generic;
using System.Linq;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;

namespace ThermoRawFileParser
{
    public class FakeMsDataFile : MsDataFile<IMzmlScan>, IMsStaticDataFile<IMzmlScan>
    {
        #region Public Constructors

        public FakeMsDataFile(String filePath, String fileName, IMzmlScan[] scans) : base(scans,
            new SourceFile("Thermo nativeID format", "Thermo RAW format", null, "", filePath, fileName))
        {
            this.Scans = scans;
        }

        #endregion Public Constructors

        #region Public Methods

        public override int GetClosestOneBasedSpectrumNumber(double retentionTime)
        {            
//            int ok = Array.BinarySearch(Scans.Select(b => b.RetentionTime).ToArray(), retentionTime);
//            if (ok < 0)
//                ok = ~ok;
//            return ok + 1;
            throw new NotImplementedException();
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