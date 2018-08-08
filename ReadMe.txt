// Database Credentials and Email Password
/*
The file [attachmentPath]\archive.txt (find attachmentPath in this app's config file) contains three discreet lines of encrypted data.
The first line is the log in credential for the HMC HEMM database which completes the connection string fragment you'll see in the config file.
The second line is the credential for the UWMC HEMM db, completing its connection string.
The third line is the email password.

The referenced library KeyMaster is used to decrypt the password at run time. There is another app called EncryptAndHash 
(\\Lapis\h_purchasing$\Purchasing\PMM IS data\HEMM Apps\Executables\) that you can use to change the password when that becomes necessary. 
To use EncryptAndHash, copy the appropriate line of encrypted text and paste it into the 'Encrypted Text' text box. Use the key 'pmmjobs' and click 'Decrypt'
*/

