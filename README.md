# DirectoryCertChecker

## Summary
Searches Active Directory for X.509 certificates and writes a report in CSV format detailing information about each certificate found. The report includes the following columns: Entry DN, Certificate DN, Serial Number, Expiry Date, Expiry Status, and Days Till Expiry. 

## Configuration
The configuration file lets you set the following:

server: The server hosting the directory to be searched.

searchBaseDNs: A list of the DNs to search below

warningPeriodInDays: The number of days before expiration that the expiry status should be changed to EXPIRING

username and password: The LDAP username and password for binding to the LDAP server




