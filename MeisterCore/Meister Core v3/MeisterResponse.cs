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
