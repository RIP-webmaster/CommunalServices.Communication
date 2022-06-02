/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunalServices.Communication.Data
{
    public class PersonInfo
    {
        public PersonInfo()
        {
            this.Imya = string.Empty;
            this.Otchestvo = string.Empty;
            this.Kod = string.Empty;
        }

        public int LS { get; set; }
        public string Familia { get; set; }
        public string Imya { get; set; }
        public string Otchestvo { get; set; }
        public DateTime? DateLeft { get; set; }
        public string Kod { get; set; }
    }

    public class DebtInfo
    {
        public DebtInfo()
        {
            this.ExtraInfo = string.Empty;
        }

        List<PersonInfo> persons = new List<PersonInfo>();

        public DebtRequest Request { get; set; }
        public bool HasCourtDebt { get; set; }

        public void AddPersonInfo(PersonInfo item)
        {
            this.persons.Add(item);
        }

        public void ClearPersonsInfo()
        {
            this.persons.Clear();
        }

        public IEnumerable<PersonInfo> GetPersonsInfo()
        {
            return this.persons.ToArray();
        }

        public string ExtraInfo { get; set; }
    }
}
