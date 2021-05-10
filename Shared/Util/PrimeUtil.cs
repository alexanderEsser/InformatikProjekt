using System;
using System.Collections.Generic;
using System.Text;

namespace Informatikprojekt_DotNetVersion.Shared.Util
{
    public class PrimeUtil
    {
        readonly static public int ANZAHL_ZAHLEN_PRO_MESSAGE = 1000;
        private static int anzahlZahlen;

        public static int AnzahlZahlen
        {
            get => anzahlZahlen;
            set => anzahlZahlen = value;
        }

        public static bool isPrimeNumber(int number) {
            if (number == 2 ) {
                return true;
            }

            if (number % 2 == 0 || number == 1) {
                return false;
            }

            for (int i = 3; i * i <= number; i += 2) {
                if (number % i == 0) {
                    return false;
                }
            }
            return true;
        }

        /**
     * Generate String-rows seperated by ',' of numbers from 0 to max
     *
     * @param max
     * @return
     */
        public static List<String> generateNumberRows(int max) {
            Console.WriteLine("Starting to generate numbers from 0 to " + max + "...");
            anzahlZahlen = max;
            List<String> numbers = new List<String>();
            StringBuilder temp = new StringBuilder();

            for (int i = 1; i <= max; i++) {
                if (i % ANZAHL_ZAHLEN_PRO_MESSAGE != 0) {
                    temp.Append(i).Append(",");
                } else {
                    temp.Append(i);
                    numbers.Add(temp.ToString());
                    temp = new StringBuilder();
                }
            }

            Console.WriteLine("Finished generating numbers");

            return numbers;
        }
    
    }
}