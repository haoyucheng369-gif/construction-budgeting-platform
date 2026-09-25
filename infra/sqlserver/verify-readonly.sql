SET NOCOUNT ON;
IF NOT EXISTS (
    SELECT 1 FROM budget.ProjectBaselines
    WHERE ProjectId = '11111111-1111-1111-1111-111111111111'
      AND Amount = 1300.00 AND Currency = 'EUR'
)
    THROW 50001, 'Expected budget baseline is missing', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    -- 即使权限配置错误，这条语句也不会修改任何数据行。
    UPDATE budget.ProjectBaselines SET Amount = Amount WHERE 1 = 0;
    ROLLBACK TRANSACTION;
    THROW 50002, 'Budget reader unexpectedly has write access', 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() <> 229 THROW;
    PRINT 'Budget baseline readable; UPDATE correctly rejected.';
END CATCH;
