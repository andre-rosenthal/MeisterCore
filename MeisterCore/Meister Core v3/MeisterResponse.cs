namespace MeisterCore.MeisterCoreV3
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
