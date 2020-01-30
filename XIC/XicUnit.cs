using System.Collections;

namespace ThermoRawFileParser.XIC
{
    public class XicUnit
    {
        public XicMeta Meta { get; set; }
        public object X { get; set; }
        public object Y { get; set; }
    
    
        public XicUnit()
        {
            Meta = new XicMeta();
            X = null;
            Y = null;
            
        }
        
        public XicUnit (double mzstart, double mzend, double rtstart, double rtend, string filter)
        {
            Meta = new XicMeta();
            Meta.MzStart = mzstart;
            Meta.MzEnd = mzend;
            Meta.RtStart = rtstart;
            Meta.RtEnd = rtend;
            Meta.Filter = filter;
        }

        public XicUnit(XicUnit copy)
        {
            Meta = new XicMeta(copy.Meta);
            
            if (copy.X != null)
            {
                if (copy.X is string)
                {
                    X = (string)copy.X;
                }
                else
                {
                    ArrayList x = new ArrayList();
                    foreach (double d in (ArrayList)copy.X)
                    {
                        x.Add(d);
                    }
                    X = x;
                }
            }
            else
            {
                X = null;
            }
            
            if (copy.Y != null)
            {
                if (copy.Y is string)
                {
                    Y = (string)copy.Y;
                }
                else
                {
                    ArrayList y = new ArrayList();
                    foreach (double d in (ArrayList)copy.Y)
                    {
                        y.Add(d);
                    }
                    Y = y;
                }
            }
            else 
            {
                Y = null;
            }
        }
    }
}
