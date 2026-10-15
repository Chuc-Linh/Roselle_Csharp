USE QuanLyBanHoa;
GO

-- Non-destructive additions required by the API and BB-DoAn.
IF COL_LENGTH('dbo.PRODUCT', 'ProductCode') IS NULL
BEGIN
    ALTER TABLE dbo.PRODUCT ADD ProductCode NVARCHAR(50) NULL;
    UPDATE dbo.PRODUCT
       SET ProductCode=N'HH'+RIGHT(N'00000'+CONVERT(NVARCHAR(5),ProductID),5)
     WHERE ProductCode IS NULL;
    ALTER TABLE dbo.PRODUCT ALTER COLUMN ProductCode NVARCHAR(50) NOT NULL;
    ALTER TABLE dbo.PRODUCT ADD CONSTRAINT UQ_PRODUCT_ProductCode UNIQUE(ProductCode);
END;
GO

IF COL_LENGTH('dbo.PRODUCT', 'SupplierName') IS NULL
    ALTER TABLE dbo.PRODUCT ADD SupplierName NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.SALES_ORDER', 'DeliveryAddress') IS NULL
    ALTER TABLE dbo.SALES_ORDER ADD DeliveryAddress NVARCHAR(200) NULL;
GO

-- The original demo seeds use placeholder values such as HASH_pass1, which
-- cannot be checked by the API. Set seeded admin and customer accounts to the
-- documented demo password so the existing database can be tested directly.
UPDATE dbo.ACCOUNT
   SET PasswordHash='pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0='
 WHERE Username IN ('admin1','admin2','admin3')
   AND Role=N'QuanTriVien'
   AND PasswordHash LIKE 'HASH_pass%';
UPDATE dbo.ACCOUNT
   SET PasswordHash='pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0='
 WHERE Role=N'KhachHang'
   AND PasswordHash LIKE 'HASH_pass%';
GO

IF OBJECT_ID('dbo.SHOPPING_CART_ITEM', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SHOPPING_CART_ITEM (
        CustomerID INT NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        CONSTRAINT PK_SHOPPING_CART_ITEM PRIMARY KEY (CustomerID, ProductID),
        CONSTRAINT FK_CART_CUSTOMER FOREIGN KEY (CustomerID) REFERENCES dbo.CUSTOMER(CustomerID) ON DELETE CASCADE,
        CONSTRAINT FK_CART_PRODUCT FOREIGN KEY (ProductID) REFERENCES dbo.PRODUCT(ProductID)
    );
END;
GO

-- The original check constraint is auto-named, so find and replace it safely.
DECLARE @constraint sysname, @sql nvarchar(max);
SELECT TOP (1) @constraint = cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('dbo.PAYMENT')
  AND cc.name <> 'CK_PAYMENT_Method_API'
  AND cc.definition LIKE '%Method%'
  AND cc.definition LIKE N'%Chuyển khoản%';
IF @constraint IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.PAYMENT DROP CONSTRAINT ' + QUOTENAME(@constraint);
    EXEC sys.sp_executesql @sql;
END;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.PAYMENT') AND name='CK_PAYMENT_Method_API')
    ALTER TABLE dbo.PAYMENT ADD CONSTRAINT CK_PAYMENT_Method_API CHECK (Method IN (N'Tiền mặt', N'Chuyển khoản', N'Trực tuyến'));
GO

-- Bring import cost in line with the weighted-average formula in BB-DoAn.
CREATE OR ALTER TRIGGER dbo.trg_GoodsReceipt_Complete
ON dbo.GOODS_RECEIPT
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(Status) RETURN;

    UPDATE p
       SET p.StockQuantity = p.StockQuantity + x.TotalQty
    FROM dbo.PRODUCT p
    JOIN (
        SELECT grd.ProductID, SUM(grd.Quantity) TotalQty
        FROM dbo.GOODS_RECEIPT_DETAIL grd
        JOIN inserted i ON i.ReceiptID=grd.ReceiptID
        JOIN deleted d ON d.ReceiptID=i.ReceiptID
        WHERE d.Status=N'Chưa hoàn thành' AND i.Status=N'Đã hoàn thành'
        GROUP BY grd.ProductID
    ) x ON x.ProductID=p.ProductID;

    UPDATE p
       SET p.CostPrice = ((p.StockQuantity-x.TotalQty)*p.CostPrice+x.ImportCost)/NULLIF(p.StockQuantity,0)
    FROM dbo.PRODUCT p
    JOIN (
        SELECT grd.ProductID, SUM(grd.Quantity) TotalQty,
               SUM(grd.Quantity*grd.ImportPrice) ImportCost
        FROM dbo.GOODS_RECEIPT_DETAIL grd
        JOIN inserted i ON i.ReceiptID=grd.ReceiptID
        JOIN deleted d ON d.ReceiptID=i.ReceiptID
        WHERE d.Status=N'Chưa hoàn thành' AND i.Status=N'Đã hoàn thành'
        GROUP BY grd.ProductID
    ) x ON x.ProductID=p.ProductID;
END;
GO
