# DirectoryCertChecker

## Summary
DirectoryCertChecker is a .NET console application that searches Active Directory for X.509 certificates and writes a report in CSV format detailing information about each certificate found. The report includes the following columns: Entry DN, Certificate DN, Serial Number, Expiry Date, Expiry Status, and Days Till Expiry. 

## Configuration
The main configuration settings for DirectoryCertChecker are held in the file DirectoryCertChecker.exe.config, which is in the same folder as DirectoryCertChecker.exe. This is an xml file that allows a number of settings to be configured as listed below. 

* server: The server hosting the directory to be searched.

* searchBaseDNs: A list of the DNs to search below

* warningPeriodInDays: The number of days before expiration that the expiry status should be changed to EXPIRING

* username and password: The LDAP username and password for binding to the LDAP server

* email recipients: you can email the report out if you wish

* email configuration: SMTP server, port etc






