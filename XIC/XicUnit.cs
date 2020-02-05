using System.Collections;

namespace ThermoRawFileParser.XIC
{
    public class XicUnit
    {
        public XicMeta Meta { get; set; }
        public object RetentionTimes { get; set; }
        public object Intensities { get; set; }


        public XicUnit()
        {
            Meta = new XicMeta();
            RetentionTimes = null;
            Intensities = null;
        }

        public XicUnit(double mzStart, double mzEnd, double? rtStart, double? rtEnd, string filter)
        {
            Meta = new XicMeta();
            Meta.MzStart = mzStart;
            Meta.MzEnd = mzEnd;
            Meta.RtStart = rtStart;
            Meta.RtEnd = rtEnd;
            Meta.Filter = filter;
        }

        public bool HasValidRanges()
        {
            var valid = !(Meta.MzStart > Meta.MzEnd);

            if (Meta.MzStart != null && Meta.RtEnd != null)
            {
                if (Meta.RtStart > Meta.RtEnd)
                {
                    valid = false;
                }
            }

            return valid;
        }

        public XicUnit(XicUnit copy)
        {
            Meta = new XicMeta(copy.Meta);

            if (copy.RetentionTimes != null)
            {
                if (copy.RetentionTimes is string)
                {
                    RetentionTimes = (string) copy.RetentionTimes;
                }
                else
                {
                    ArrayList x = new ArrayList();
                    foreach (double d in (ArrayList) copy.RetentionTimes)
                    {
                        x.Add(d);
                    }

                    RetentionTimes = x;
                }
            }
            else
            {
                RetentionTimes = null;
            }

            if (copy.Intensities != null)
            {
                if (copy.Intensities is string)
                {
                    Intensities = (string) copy.Intensities;
                }
                else
                {
                    ArrayList y = new ArrayList();
                    foreach (double d in (ArrayList) copy.Intensities)
                    {
                        y.Add(d);
                    }

                    Intensities = y;
                }
            }
            else
            {
                Intensities = null;
            }
        }
    }
}