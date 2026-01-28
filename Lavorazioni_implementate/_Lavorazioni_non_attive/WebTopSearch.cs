using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Data;
using System.Text.RegularExpressions;


namespace LibraryLavorazioni
{
    /// <summary>
    /// Recupera i dati di produzione tramite Webtop
    /// </summary>
    public class WebTopSearch
    {
        public IWebDriver WebDriver;
        public string UrlLavorazione { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public DateTime DataLavorazione { get; set; }
        public string NomeLavorazione { get; set; }

        public WebTopSearch() { }


        public DataTable FillTable()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string strPathChromePortable = System.IO.Path.Combine(strWorkPath, "chrome.exe");

            var chromeOptions = new ChromeOptions();
            //chromeOptions.BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            chromeOptions.BinaryLocation = strPathChromePortable;
            //chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArguments("--disable-popup-blocking");
            chromeOptions.AcceptInsecureCertificates = true;

            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(chromeDriverService, chromeOptions, TimeSpan.FromSeconds(180));

            try
            {
                driver.Manage().Cookies.DeleteAllCookies();
                driver.Navigate().GoToUrl(UrlLavorazione);

                //var wait = new WebDriverWait(driver, new TimeSpan(0, 0, 30));
                //var el = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.AlertIsPresent());

                WaitForDocumentReady(driver, TimeSpan.FromSeconds(120));

                LogInAsUser(driver);

                if (isAlertPresent(driver) == true)
                {
                    driver.SwitchTo().Alert().Accept();
                }

                //wait load page
                WaitForDocumentReady(driver, TimeSpan.FromMinutes(2));

                driver.SwitchTo().DefaultContent();
                driver.SwitchTo().Frame("CustomMain_view_0");
                driver.SwitchTo().Frame("Classic_browser_1");
                driver.SwitchTo().Frame("PostelBrowserTree_customTree_0");

                driver.FindElement(By.XPath("//a[@id ='customTree4']")).Click();

                driver.SwitchTo().DefaultContent();
                driver.SwitchTo().Frame("CustomMain_view_0");
                driver.SwitchTo().Frame("Classic_browser_1");
                driver.SwitchTo().Frame("PostelBrowserTree_customTree_0");

                driver.FindElement(By.XPath("//a[@id ='customTree6']")).Click();


                /******** form di ricerca ************/

                driver.SwitchTo().DefaultContent();
                driver.SwitchTo().Frame("CustomMain_view_0");
                driver.SwitchTo().Frame("Classic_workarea_0");


                //wait load page
                WaitForDocumentReady(driver, TimeSpan.FromMinutes(2));

                //eseguo ricerca
                Ricerca(driver, this.DataLavorazione);


                //get number of pages
                IList<IWebElement> allElement = driver.FindElements(By.ClassName("defaultDataPagingStyle"));

                int numberPages = 0;
                foreach (IWebElement element in allElement)
                {
                    string paging = element.Text;
                    var resultString = Regex.Match(paging, @"\d+").Value;
                    numberPages = Int32.Parse(resultString);
                }


                //get columns name and 
                IList<IWebElement> allColums = driver.FindElements(By.ClassName("doclistbodyDatasortlink"));
                int numberColumns = allColums.Count();
                DataTable tableData = new DataTable();

                foreach (IWebElement element in allColums)
                {
                    string col = element.Text;
                    tableData.Columns.Add(col, typeof(string));
                }



                DataTable table = StampaTabella(driver, numberPages, tableData, numberColumns);

                driver.SwitchTo().DefaultContent();
                driver.SwitchTo().Frame("CustomMain_titlebar_0");

                WaitUntilElementClickable(driver, By.Name("CustomTitlebarContributor_logout_0"));

                driver.FindElement(By.Name("CustomTitlebarContributor_logout_0")).SendKeys(OpenQA.Selenium.Keys.Return);

                return table;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                //Closing browser
                //driver.Close();
                driver.Quit();
                chromeDriverService.Dispose();
            }


        }


        private bool isAlertPresent(IWebDriver driver)
        {
            try
            {
                driver.SwitchTo().Alert();
                return true;
            }   // try 
            catch (NoAlertPresentException Ex)
            {
                return false;
            }
        }

        private void Ricerca(IWebDriver driver, DateTime dataRicerca)
        {

            new WebDriverWait(driver, TimeSpan.FromSeconds(180)).Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//input[contains(@id, 'dataDa_date')]")));
            new WebDriverWait(driver, TimeSpan.FromSeconds(180)).Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//input[contains(@id, 'dataA_date')]")));

            IWebElement dateDa = driver.FindElement(By.XPath("//input[contains(@id, 'dataDa_date')]"));
            IWebElement dateTo = driver.FindElement(By.XPath("//input[contains(@id, 'dataA_date')]"));


            dateDa.Clear();
            dateDa.SendKeys(dataRicerca.Date.ToString("dd/MM/yyyy"));

            dateTo.Clear();
            dateTo.SendKeys(dataRicerca.Date.ToString("dd/MM/yyyy"));

            driver.FindElement(By.Name("ReportLavorazioniOperatoriComponent_Button_0")).SendKeys(OpenQA.Selenium.Keys.Return);
        }

        private void LogInAsUser(IWebDriver driver)
        {

            IWebElement username = driver.FindElement(By.XPath("//input[contains(@id, 'LoginUsername')]"));
            IWebElement password = driver.FindElement(By.XPath("//input[contains(@id, 'LoginPassword')]"));

            username.Clear();
            username.SendKeys(this.User);

            password.Clear();
            password.SendKeys(this.Password);


            driver.FindElement(By.Name("PostelLogin_loginButton_0")).SendKeys(OpenQA.Selenium.Keys.Return);


        }

        private void WaitForDocumentReady(IWebDriver driver, TimeSpan waitTime)
        {
            if (isAlertPresent(driver) == true)
            {
                driver.SwitchTo().Alert().Accept();
            }

            var wait = new WebDriverWait(driver, waitTime);
            var javascript = driver as IJavaScriptExecutor;
            if (javascript == null)
                throw new ArgumentException("driver", "Driver must support javascript execution");

            wait.Until((d) =>
            {
                try
                {
                    if (isAlertPresent(driver) == true)
                    {
                        driver.SwitchTo().Alert().Accept();
                    }

                    string readyState = javascript.ExecuteScript(
                        "if (document.readyState) return document.readyState;").ToString();
                    return readyState.ToLower() == "complete";
                }
                catch (InvalidOperationException e)
                {
                    return e.Message.ToLower().Contains("unable to get browser");
                }
                catch (WebDriverException e)
                {
                    return e.Message.ToLower().Contains("unable to connect");
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }


        private static IWebElement WaitUntilElementClickable(IWebDriver driver, By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return wait.Until(ExpectedConditions.ElementToBeClickable(elementLocator));

            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator: '" + elementLocator + "' was not found in current context page.");
                throw;
            }
        }


        private DataTable StampaTabella(IWebDriver driver, int numberPages, DataTable tableData, int numberColumns)
        {


            //get page as needed
            for (int i = 0; i <= numberPages; i++)
            {

                //get the result table
                var table = driver.FindElement(By.ClassName("datagrid"));
                var rows = table.FindElements(By.TagName("tr"));
                foreach (var row in rows)
                {

                    var className = row.GetAttribute("class");
                    if (className == "contentBackground")
                    {
                        //var rowTds = row.FindElements(By.TagName("td")).ToList();
                        //var r = tableData.NewRow();
                        ////fill table with data
                        //for (i = 0; i < numberColumns; i++)
                        //{
                        //    r[i] = rowTds[i].Text;
                        //}

                        var rowTds = row.FindElements(By.TagName("td")).ToList();
                        var r = tableData.NewRow();

                        if (rowTds.Count > 1)
                        {
                            ////fill table with data
                            for (i = 0; i < numberColumns; i++)
                            {

                                r[i] = rowTds[i].Text;
                            }

                            tableData.Rows.Add(r);
                        }

                    }

                }

                try
                {
                    driver.FindElement(By.Name("ReportLavorazioniOperatoriComponent_pager1_next_0")).SendKeys(OpenQA.Selenium.Keys.Return);
                    WaitForDocumentReady(driver, TimeSpan.FromSeconds(120));
                }
                catch (NoSuchElementException ex)
                {
                    continue;
                }


            }

            //debug
            //XLWorkbook wb = new XLWorkbook();
            //wb.Worksheets.Add(tableData, "lavorazione");
            //wb.SaveAs(Application.StartupPath + "\\" + this.NomeLavorazione + ".xlsx");

            return tableData;
        }
    }
}
