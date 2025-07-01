// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("eeQ3KMZkwZ7TdLArYJkFNAirYtW3TQi3zeQo+Yp/FyTZi0ZL4djCQqvg3sIN62JV6BP8DyxHvDRqFP3BMoADIDIPBAsohEqE9Q8DAwMHAgGtDJrIUHupikXGlWEkWtkKU+eDVHUd600Siv4prlB2Xz6pl+s4h0wYM+T5zgm4h8pMyhH6QbcX/YbFLPxcc0QTj/ZSR7KLn38svf7pzOCj5GjRhd1dRAFyjG4zpAUvRE/I+cPoUl1dvRMtHuOC9gihqd0Rai0znVaAAw0CMoADCACAAwMC7Jo25057usqwSWzs8sjWUIa247dUBDGPTg/7hhrP/ah1igrNVjaQHV6obtQVn0eOfBy5cKqqO3LQuLuT0St41yL9Mxk3ev1QDDXReQABAwID");
        private static int[] order = new int[] { 10,12,13,10,4,7,6,10,8,11,12,13,13,13,14 };
        private static int key = 2;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
