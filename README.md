# middle-notes.saver

Part 1 of the Data Scraping Architecture that I use to mine data from webistes.
In this solution (called Saver) I wrote the Console Application to load a list of Web Pages and store their Html into Db tables.
The second part (in progress) uses HtmlAgilityPack and XPath syntax to extract data from the Web Pages saved before, using multiple DB configs.

# The Saver
Simple project about Data Scraping, using TPL (Task Parallel Library), managing Tasks and multiple Selenium instances, with a support for MySql/MariaDb database for storing Web Pages.
