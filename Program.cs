using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace PostTransactions
{
    using System.Data.Common;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Xml;

    using Newtonsoft.Json;

    public class TransDetail
    {
        public Int64 Id { get; set; }
        public String TicketNumber { get; set; }
        public string UserStationName { get; set; }
        public string FacilityIdenifier { get; set; }
        public string AccountNumber { get; set; }
        public DateTime Transactiondate { get; set; }
        public string TransactionCode { get; set; }
        public string TransactionsType { get; set; }
        public decimal TransactionAmount { get; set; }
        public string AssociatedPayor { get; set; }
        public string TransactionDescription { get; set; }
        public string InboundFile { get; set; }
        public DateTime DatePlaced { get; set; }
        public DateTime DateEntered { get; set; }
        public string FileSourceID { get; set; }
        public DateTime? FileSent { get; set; }
        public DateTime? DateTimeSent { get; set; }
        public string InboundFileName { get; set; }
        public string OutboundFileName { get; set; }
        public Int32? Status { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var testme = new DoThis();

            var fred = testme.RunAsync();
            Console.ReadLine();
        }

    }

    public class DoThis
    {
        private static HttpClient client = new HttpClient();

        public async Task RunAsync()
        {
            client.BaseAddress = new Uri("http://localhost:60686/");
            int othercount = 0;
            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/"));
            try
            {
                TransDetail transDetail = null;
                List<TransDetail> transDetailAll;
                // Get the product

                transDetail = await GetTransAsync("http://localhost:60686/api/TransactionDataAPI/5/4561");
                if (transDetail != null)
                {
                    ShowTrans(transDetail);
                }
                else
                {
                    Console.WriteLine("Record not found");
                }

                int transcount =  AddTransFile(@"C:\Developer\CASH\Save\TransActtionFile1.txt");

                
                transDetailAll = await GetAllTransAsync("http://localhost:60686/api/TransactionDataAPI");
                Console.WriteLine(string.Format("Record Count = {0}", transDetailAll.Count));
                
                foreach (TransDetail detail in transDetailAll)
                {
                    ShowTrans(detail);
                    othercount ++;
                }

            }
            catch (Exception)
            {

                throw;
            }

            Console.WriteLine(string.Format("Final Record Count = {0}", othercount));

            Console.ReadLine();

        }

        private static void ShowTrans(TransDetail pTransDetail)
        {
            Console.WriteLine(string.Format("Account Number: {0}\tTrans Date: {1:MMddyyyy}\tTransAmount: {2:C}\tTransCode: {3}",
                pTransDetail.AccountNumber, pTransDetail.Transactiondate, pTransDetail.TransactionAmount, pTransDetail.TransactionCode));
        }

        private static async Task<TransDetail> GetTransAsync(string path)
        {
            TransDetail transDetail = null;

            HttpResponseMessage response = await client.GetAsync(path);
            //var resultnote = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string stringData = response.Content.ReadAsStringAsync().Result;
                    transDetail = JsonConvert.DeserializeObject<TransDetail>(stringData);
                }
                catch (Exception e)
                {

                    throw;
                }

            }
            return transDetail;
        }

        private int AddTransFile(string pFile)
        {
            int transCount = 0;

            if (File.Exists(pFile))
            {
                StreamReader reader = null;
                try
                {
                    reader = new StreamReader(pFile);
                    using (reader)
                    {
                        string transline = string.Empty;
                        while (!reader.EndOfStream)
                        {
                            transline = reader.ReadLine();
                            var result = AddTransactionFromFile(transline, pFile);
                            transCount++;
                        }
                    }
                }
                catch (DbException)
                {
                    throw;
                }
            }
            return transCount;
        }

        private static async Task<Uri> AddTransactionFromFile(string pTransactionLine, string pFileDetails)
        {
            string[] details = pTransactionLine.Split('\t');
            TransDetail transDetail = new TransDetail();
            DateTime filedate = File.GetCreationTime(pFileDetails);
            string filenameext = Path.GetFileName(pFileDetails);
            string filelocation = Path.GetDirectoryName(pFileDetails);
            decimal transamount = 0;
            if (details[7].Trim().Length > 0)
            {
                transamount = Convert.ToDecimal(details[7]);
            }

            DateTime transactionDateTime = DateTime.ParseExact(details[2], "yyyyMMdd", CultureInfo.InvariantCulture);

            //transDetail.Id = 10;
            transDetail.AccountNumber = details[1];
            transDetail.UserStationName = "Jim";
            transDetail.TicketNumber = "TKT File";
            transDetail.FacilityIdenifier = "testFacility";
            transDetail.TransactionCode = details[6];
            transDetail.Transactiondate = transactionDateTime;
            transDetail.TransactionAmount = transamount;
            transDetail.TransactionsType =  details[6];
            transDetail.AssociatedPayor = string.Format("{0}, {1} {2}", details[3].Trim(), details[4].Trim(), details[5].Trim()).Trim();
            transDetail.InboundFile = filenameext;
            transDetail.Status = 1;
            transDetail.TransactionDescription = details[10];
            transDetail.DateEntered = DateTime.Now;
            transDetail.DatePlaced = filedate;
            transDetail.FileSourceID = "sourceid33";

            var url = await AddTransactionAsync(transDetail);
            return url;

        }

        private static async Task<List<TransDetail>> GetAllTransAsync(string path)
        {
            List<TransDetail> transDetail = null;

            HttpResponseMessage response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string stringData = response.Content.ReadAsStringAsync().Result;
                    transDetail = JsonConvert.DeserializeObject<List<TransDetail>>(stringData);
                }
                catch (Exception e)
                {

                    throw;
                }

            }
            return transDetail;
        }

        static async Task<Uri> AddTransactionAsync(TransDetail pTransDetail)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/TransactionDataAPI", pTransDetail);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            return response.Headers.Location;
        }

    }
}
