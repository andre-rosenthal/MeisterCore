using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeisterCore.Meister_Core_v3
{
    public class MeisterResponse<RES>
    {
        private MeisterException meisterException = null;
        public RES Response { get; set; }
        public MeisterException MeisterException
        {
            get 
            {
                return meisterException;
            }
        }
    }
}
