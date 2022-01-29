using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;


namespace SiteParser
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> urls = new List<string>();//список элементов, которых нет в sitemap.xml
            List<string> urlsfile = new List<string>();//список ссылок в sitemap.xml
            List<UrlLink> allurl = new List<UrlLink>();//cсоздаем список сех ссылок
            urls.Add(Console.ReadLine());
            Console.WriteLine("\nin processing...");

            for (int i = 0; i < urls.Count(); i++)
            {
                try
                {
                    if(!urls[i].ToString().Contains(urls[0]))
                    {
                        continue;
                    }

                    WebRequest request = WebRequest.Create(urls[i]);//отправляемс запрос на ссылку 
                    WebResponse response = request.GetResponse();//получаем ответ 
                    Stream data = response.GetResponseStream();//создаем поток ответов
                    string html = String.Empty;//штмл разметка в виде строки 
                    using (StreamReader sr = new StreamReader(data))
                    {
                        html = sr.ReadToEnd();
                    }
                    var parser = new HtmlParser();
                    var document = parser.ParseDocument(html);
                    var links = document.QuerySelectorAll("a, img, link, script");
                    //возвращаем строки с атрибутом <а>

                    foreach (var link in links)
                    {

                        var url = link.GetAttribute("href");//получаем ссылки с атрибутом <href>
                        if (string.IsNullOrEmpty(url) && !url.Contains("mailto") && !url.Contains("ssh") && !url.Contains("tel"))
                        {
                            url = link.GetAttribute("src");
                            if(string.IsNullOrEmpty(url))
                            {
                                continue;//пропускаем пустые ссылки 
                            }
                        }
                        
                        if (url.StartsWith('/'))
                        {
                            url = $"{urls[0].TrimEnd('/')}{url}";
                        }
                        Uri uri;
                        if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                        {
                            
                        }
                        // добавляем проверку на: пустые ссылки, содержание юзер домена, наличие в списке
                        if (uri != null  && !urls.Contains(uri.ToString()) && !urlsfile.Contains(uri.ToString()))
                            urls.Add(uri.ToString());
                    }
                }
                catch (Exception e)
                {
                    continue;//продолжаем если содержит ошибку, чтобы цикл не продолжался по новой
                }

            }
            try
            {
                // работа с сайтмапом
                WebClient wc = new WebClient();
                wc.Encoding = System.Text.Encoding.UTF8;
                if (!urls[0].StartsWith("https://") || !urls[0].StartsWith("https://") && !urls[0].StartsWith("http://"))
                {
                    urls[0] = "https://" + urls[0];
                }
                //если домен заканчивается не заканчивается" / ", добавляем его,
                if (!urls[0].EndsWith("/"))
                {
                    urls[0] = urls[0] + "/";
                }
                string sitemapString = wc.DownloadString(urls[0] + "sitemap.xml");
                XmlDocument urldoc = new XmlDocument();
                urldoc.LoadXml(sitemapString);
                XmlNodeList xmlSitemapList = urldoc.GetElementsByTagName("url");
                foreach (XmlNode node in xmlSitemapList)//перебираем все ссылки
                {
                    if (node["loc"] != null)
                    {
                        urlsfile.Add(node["loc"].InnerText);//добавляем в список
                    }
                }
            }

            catch (Exception e)
            {

            }
            //вывод ссылок которые были найдены сайтмапом
            Console.WriteLine("\n\nUrls found in sitemap but not founded after crawling of web site");
            var crowLingLinks = urls.Except(urlsfile)?.ToList();
            var sitemapLinks = urlsfile.Except(crowLingLinks)?.ToList();
            Console.WriteLine("\nlinks count=" + sitemapLinks.Count());//счетчик ссылок \(**)/
            //создаем цыкл с выводом ссылок сайтмапа, которых не ообнаружили при кроулинге
            int b = 1;
            for (int i = 0; i < sitemapLinks.Count(); i++)
            {
                if (!crowLingLinks.Contains(sitemapLinks[i]))
                {
                    Console.WriteLine((b) + ") " + sitemapLinks[i]);
                    b++;//фиксируем количесвтов ссылок
                }
            }
            //убираем юзер ссылку
            urls.Remove(urls[0]);

            Console.WriteLine("\n\nUrls which were found by crawling but not in sitemap");
            Console.WriteLine("\nlinks counting=" + crowLingLinks.Count()); ;

            //создаем цыкл с выводом ссылок, которых нет в сайтмапе, нобыли найдены кроулингом  и фиксируем их
            int c = 1;
            for (int i = 0; i < crowLingLinks.Count(); i++)
            {
                if (!sitemapLinks.Contains(crowLingLinks[i]))
                {
                    Console.WriteLine((c) + ") " + crowLingLinks[i]);
                    c++;
                }
            }

            //добавляем данные из одного списка urls в другой urlsfile
            for (int i = 0; i < crowLingLinks.Count(); i++)
            {
                sitemapLinks.Add(urls[i]);
            }
            
            Console.WriteLine("\nnext step... just waiting");

            //создаем цикл для подсчета временни запроса на каждую ссылку
            for (int i = 0; i < sitemapLinks.Count(); i++)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sitemapLinks[i]);//запрос
                    Stopwatch timer = new Stopwatch();//запуск таймера
                    timer.Start();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();//ответ
                    timer.Stop();//остановка таймера
                    var urlLink = new UrlLink();
                    urlLink.Counter = int.Parse(timer.ElapsedMilliseconds.ToString());
                    urlLink.NameUrl = sitemapLinks[i];
                    allurl.Add(urlLink);//добавляем в список всех ссылок
                }
                catch (Exception e)
                {
                    
                }
            }

            Console.WriteLine("\n\nTime allotted for links");
            Console.WriteLine("\ncounting=" + allurl.Count());
            //с помощью linq сортируем по возрастанию  
            var sortedLinks = allurl.OrderBy(x => x.Counter);
            //перебираем список и выводим отсортированные ссылки
            foreach (UrlLink a in sortedLinks)
                Console.WriteLine(a.NameUrl + "   " + a.Counter + "ms");
            Console.WriteLine("\nUrls && documents which were found after crawling of website:" + (c - 1));
            Console.WriteLine("\nUrls which were found in sitemap: " + (b - 1));

        }
    }
}
