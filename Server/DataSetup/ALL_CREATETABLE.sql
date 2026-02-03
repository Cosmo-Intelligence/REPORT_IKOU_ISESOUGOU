
spool all_createtable.log

prompt --CREATETABLE

@CREATETABLE\IKOU_TEMP_TABLE.sql
commit
/

spool off
QUIT;
