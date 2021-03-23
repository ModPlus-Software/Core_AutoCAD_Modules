namespace ModPlus
{
    /// <summary>
    /// Данные, зависящие от версии
    /// </summary>
    internal class VersionData
    {
#if A2013
        public const string CurrentCadVersion = "2013";
        public const string CurrentCadInternalVersion = "19.0";
#elif A2014
        public const string CurrentCadVersion = "2014";
        public const string CurrentCadInternalVersion = "19.1";
#elif A2015
        public const string CurrentCadVersion = "2015";
        public const string CurrentCadInternalVersion = "20.0";
#elif A2016
        public const string CurrentCadVersion = "2016";
        public const string CurrentCadInternalVersion = "20.1";
#elif A2017
        public const string CurrentCadVersion = "2017";
        public const string CurrentCadInternalVersion = "21.0";
#elif A2018
        public const string CurrentCadVersion = "2018";
        public const string CurrentCadInternalVersion = "22.0";
#elif A2019
        public const string CurrentCadVersion = "2019";
        public const string CurrentCadInternalVersion = "23.0";
#elif A2020
        public const string CurrentCadVersion = "2020";
        public const string CurrentCadInternalVersion = "23.1";
#elif A2021
        public const string CurrentCadVersion = "2021";
        public const string CurrentCadInternalVersion = "24.0";
#elif A2022
        public const string CurrentCadVersion = "2022";
        public const string CurrentCadInternalVersion = "24.1";
#endif
    }
}
