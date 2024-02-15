using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace selenium
{
    public class UserContactInfo : BasicUserProfile
    {
        public string LinkedInProfile { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string IM { get; set; }
        public string Birthday { get; set; }
    }

    public class BasicUserProfile
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public string FollowersAndConnection { get; set; }
        public string Picture { get; set; }
        public string CoverImage { get; set; }
        public string About { get; set; }
        public string Role { get; set; }
    }


    public class Program
    {
        static async Task Main(string[] args)
        {
            var path = @"D:/Experiments/selenium/selenium/chromedriver-win64/chromedriver.exe";
            ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService(path);
            IWebDriver driver = new ChromeDriver(chromeDriverService, new ChromeOptions());

            try
            {
                driver.Navigate().GoToUrl("https://linkedin.com");

                var username = driver.FindElement(By.Id("session_key"));
                var userpassword = driver.FindElement(By.Id("session_password"));
                var signInButton = driver.FindElement(By.CssSelector("button.btn-md.btn-primary"));

                username.SendKeys(LinkedUserAcount.Username);
                userpassword.SendKeys(LinkedUserAcount.Password);
                signInButton.Click();

                await Task.Delay(40000); // Adjust delay as needed

                string targetProfileUrl = "https://www.linkedin.com/in/love-babbar-38ab2887/";

                Queue<string> profileQueue = new Queue<string>();
                profileQueue.Enqueue(targetProfileUrl);

                HashSet<string> visitedProfiles = new HashSet<string>();

                while (profileQueue.Count > 0)
                {
                    string currentProfileUrl = profileQueue.Dequeue();

                    if (!visitedProfiles.Contains(currentProfileUrl))
                    {
                        UserContactInfo profile = await ScrapeUserProfile(driver, currentProfileUrl);
                        string prompt = $"You have User About : {profile.About} and Title : {profile.Title} and Current Company : {profile.Company} Based on this input, the program should determine the user's role. If the user's role matches any roles from a given list of personas, the program should insert the crawled data into the database. If there's no match, the program should return without saving the data.\r\n\r\nList of Persona Roles:\r\n\r\nStudent\r\nTeacher\r\nProfessor\r\nInvestor\r\nVenture Capitalist\r\nAngel Investor\r\nFounder (CoFounder)\r\nExecutive\r\nManager\r\nLawyer\r\nAccountant\r\nBanker\r\nInstructions:\r\n\r\nPrompt the user to input their title and name.\r\nParse the user's role from the title input.\r\nCheck if the user's role matches any persona roles from the given list.\r\nIf there's a match,Set the Role and insert the crawled data into the database.\r\nIf there's no match, return without saving the data.";
                        
                        if (profile != null)
                        {
                            Console.WriteLine("Name: " + profile.Name);
                            Console.WriteLine("Title: " + profile.Title);
                            Console.WriteLine("Company: " + profile.Company);
                            Console.WriteLine("Location: " + profile.Location);
                            Console.WriteLine("Followers and Connection: " + profile.FollowersAndConnection);
                            Console.WriteLine("Profile Picture: " + profile.Picture);
                            Console.WriteLine("Cover Image: " + profile.CoverImage);
                            Console.WriteLine("About: " + profile.About);

                            foreach (var url in ScrapeUniqueLinkedInProfileUrls(driver, currentProfileUrl))
                            {
                                if (!visitedProfiles.Contains(url))
                                {
                                    profileQueue.Enqueue(url);
                                }
                            }
                        }
                        visitedProfiles.Add(currentProfileUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: " + ex.Message);
            }
            finally
            {
                driver.Quit();
            }
        }

        static async Task<UserContactInfo> ScrapeUserProfile(IWebDriver driver, string profileUrl)
        {
            try
            {
                driver.Navigate().GoToUrl(profileUrl);
                await Task.Delay(5000); // Adjust delay as needed

                UserContactInfo userContact = new UserContactInfo();
                userContact.Name = GetTextContent(driver, "h1.text-heading-xlarge.inline.t-24.v-align-middle.break-words");
                userContact.Title = GetTextContent(driver, "div.text-body-medium.break-words");
                userContact.Company = GetTextContent(driver, "ul.pv-text-details__right-panel li");
                userContact.About = GetTextContent(driver, "div.display-flex.ph5.pv3 div.pv-shared-text-with-see-more");
                userContact.Location = GetTextContent(driver, "span.text-body-small.inline.t-black--light.break-words");
                userContact.FollowersAndConnection = GetTextContent(driver, "ul.pv-top-card--list-bullet");
                userContact.Picture = GetAttribute(driver, "img.pv-top-card-profile-picture__image", "src");
                userContact.CoverImage = GetAttribute(driver, "img.profile-background-image__image", "src");

                var userdata = await ScrapeUserContactInfo(driver, userContact, profileUrl);

                return userdata;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scraping user profile: " + ex.Message);
                return null;
            }
        }
        static async Task<UserContactInfo> ScrapeUserContactInfo(IWebDriver driver, UserContactInfo contactInfo, string profileUrl)
        {
            try
            {
                var contactInfoButton = driver.FindElement(By.Id("top-card-text-details-contact-info"));
                if (contactInfoButton != null)
                {
                    contactInfoButton.Click();
                    await Task.Delay(5000);
                }

                string linkedInProfile = null;

                //var linkedInPersonalProfileElement = driver.FindElement(By.CssSelector(".pv-contact-info-section__edit-action"));
                //if (linkedInPersonalProfileElement != null)
                //    linkedInProfile = linkedInPersonalProfileElement.GetAttribute("href");

                //if (linkedInProfile == null)
                //{
                //    var linkedInUserProfileElement = driver.FindElement(By.CssSelector(".pv-contact-info__contact-type a[data-control-name='contact_see_more']"));
                //    if (linkedInUserProfileElement != null)
                //        linkedInProfile = linkedInUserProfileElement.GetAttribute("href");
                //}

                contactInfo.LinkedInProfile = profileUrl;

                var contactSections = driver.FindElements(By.CssSelector(".pv-contact-info__contact-type"));

                foreach (var section in contactSections)
                {
                    var header = section.FindElement(By.CssSelector(".pv-contact-info__header")).Text.Trim();
                    var infoContainer = section.FindElement(By.CssSelector(".pv-contact-info__ci-container"));

                    switch (header)
                    {
                        case "Your Profile":
                            contactInfo.LinkedInProfile = infoContainer.FindElement(By.CssSelector("a")).GetAttribute("href");
                            break;
                        case "Phone":
                            contactInfo.Phone = infoContainer.FindElement(By.CssSelector("span")).Text.Trim();
                            break;
                        case "Address":
                            contactInfo.Address = infoContainer.FindElement(By.CssSelector("a")).Text.Trim();
                            break;
                        case "Email":
                            contactInfo.Email = infoContainer.FindElement(By.CssSelector("a")).Text.Trim();
                            break;
                        case "IM":
                            contactInfo.IM = infoContainer.FindElement(By.CssSelector("span")).Text.Trim();
                            break;
                        case "Birthday":
                            contactInfo.Birthday = infoContainer.FindElement(By.CssSelector("span")).Text.Trim();
                            break;
                    }
                }

                return contactInfo;
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine("Error scraping contact info: Element not found - " + ex.Message);
                return contactInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scraping contact info: " + ex.Message);
                return contactInfo;
            }
        }

        static string GetTextContent(IWebDriver driver, string selector)
        {
            try
            {
                IWebElement element = driver.FindElement(By.CssSelector(selector));
                return element?.Text.Trim() ?? "Not able to Found";
            }
            catch (NoSuchElementException)
            {
                return "Not able to Found";
            }
        }

        static string GetAttribute(IWebDriver driver, string selector, string attributeName)
        {
            try
            {
                IWebElement element = driver.FindElement(By.CssSelector(selector));
                return element?.GetAttribute(attributeName)?.Trim() ?? "Not Able to Found";
            }
            catch (NoSuchElementException)
            {
                return "Not Able to Found";
            }
        }

        static IEnumerable<string> ScrapeUniqueLinkedInProfileUrls(IWebDriver driver, string currentProfileUrl)
        {
            HashSet<string> uniqueProfileUrls = new HashSet<string>();

            var profileLinkElements = driver.FindElements(By.CssSelector("a.optional-action-target-wrapper"));

            foreach (var linkElement in profileLinkElements)
            {
                string profileUrl = linkElement.GetAttribute("href");

                if (profileUrl.Contains('?'))
                {
                    int indexofQuestionMark = profileUrl.IndexOf('?');
                    profileUrl = profileUrl.Substring(0, indexofQuestionMark);
                }

                if (profileUrl.Contains("https://www.linkedin.com/in/") && !profileUrl.Contains(currentProfileUrl))
                {
                    uniqueProfileUrls.Add(profileUrl);
                }
            }

            return uniqueProfileUrls;
        }
    }
}
