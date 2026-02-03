@echo off

rem **********************************************
rem パラメータ設定 ユーザ定義
rem sys_conn:			オラクル接続文字列 ※システムユーザID/パスワード@TNS名
rem conn:				オラクル接続文字列 ※ユーザID/パスワード@TNS名
rem **********************************************
rem set sys_conn=system/rqris_system@MRMS
rem set conn=mrms/mrms1@MRMS
set sys_conn=system/mochi
set conn=ikou/ikou1
set tablespace_data=MRMS_DATA
set tablespace_temp=MRMS_TEMP
set tablespace_index=MRMS_INDEX

echo データ移行ツールのセットアップを開始します
pause

set NLS_LANG=Japanese_Japan.JA16SJISTILDE

echo 移行用ユーザを作成します

rem **********************************************
rem ユーザーの作成
rem **********************************************
sqlplus -l %sys_conn% @CreateUser.sql %tablespace_data% %tablespace_temp% %tablespace_index%

echo テーブルを作成します

rem **********************************************
rem テーブルの作成
rem **********************************************
sqlplus -l %conn% @ALL_CREATETABLE.sql %tablespace_data% %tablespace_temp% %tablespace_index%

echo セットアップが完了しました。各ログでエラーが発生していないことを確認してください
pause

QUIT;