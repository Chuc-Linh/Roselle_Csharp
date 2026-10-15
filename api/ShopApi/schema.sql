-- =====================================================
-- QuanLyBanHoa - BẢN RÚT GỌN (CHUẨN 3NF)
-- =====================================================
USE master;
GO

IF DB_ID('QuanLyBanHoa') IS NOT NULL
    THROW 50000, N'Database QuanLyBanHoa đã tồn tại. Để giữ dữ liệu, không chạy lại file tạo mới; hãy dùng migrations/001-api-compat.sql.', 1;
CREATE DATABASE QuanLyBanHoa;
GO
USE QuanLyBanHoa;
GO

-- =====================================================
-- 1. ACCOUNT
-- =====================================================
CREATE TABLE ACCOUNT (
    AccountID    INT IDENTITY(1,1) PRIMARY KEY,
    Username     VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role         NVARCHAR(30) NOT NULL
                 CHECK (Role IN (N'KhachHang', N'QuanTriVien')),
    Status       BIT  NOT NULL DEFAULT 1,
    CreatedDate  DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- =====================================================
-- 2. CUSTOMER
-- =====================================================
CREATE TABLE CUSTOMER (
    CustomerID       INT IDENTITY(1,1) PRIMARY KEY,
    FullName         NVARCHAR(100) NOT NULL,
    Gender           NVARCHAR(10)  CHECK (Gender IN (N'Nam', N'Nữ', N'Khác')),
    DateOfBirth      DATE,
    Email            VARCHAR(100),
    PhoneNumber      VARCHAR(15)  UNIQUE,
    Address          NVARCHAR(200),
    RegistrationDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    AccountID        INT  NOT NULL UNIQUE,
    FOREIGN KEY (AccountID) REFERENCES ACCOUNT(AccountID)
);
GO
CREATE UNIQUE INDEX UX_CUSTOMER_Email ON CUSTOMER(Email) WHERE Email IS NOT NULL;
GO

-- =====================================================
-- 3. PRODUCT_TYPE
-- =====================================================
CREATE TABLE PRODUCT_TYPE (
    ProductTypeID   INT IDENTITY(1,1) PRIMARY KEY,
    ProductTypeName NVARCHAR(50) NOT NULL UNIQUE,
    Description     NVARCHAR(200),
    Status          BIT NOT NULL DEFAULT 1
);
GO

-- =====================================================
-- 4. PRODUCT
-- =====================================================
CREATE TABLE PRODUCT (
    ProductID     INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode   NVARCHAR(50) NULL,
    ProductName   NVARCHAR(100) NOT NULL,
    ProductTypeID INT NOT NULL,
    Description   NVARCHAR(500),
    Color         NVARCHAR(50),
    Unit          NVARCHAR(20)  NOT NULL,
    CostPrice     DECIMAL(18,2) NOT NULL CHECK (CostPrice >= 0),
    ProfitRate    DECIMAL(5,2)  NOT NULL CHECK (ProfitRate >= 0),
    SellingPrice  AS (CAST(CostPrice * (1 + ProfitRate/100.0) AS DECIMAL(18,2))),
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    MinStockLevel INT NOT NULL DEFAULT 5 CHECK (MinStockLevel >= 0),
    Image         NVARCHAR(255),
    SupplierName  NVARCHAR(100),
    Status        BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (ProductTypeID) REFERENCES PRODUCT_TYPE(ProductTypeID)
);
GO

-- =====================================================
-- 5. GOODS_RECEIPT  (ĐÃ XÓA TotalAmount - chuẩn 3NF)
-- =====================================================
CREATE TABLE GOODS_RECEIPT (
    ReceiptID   INT IDENTITY(1,1) PRIMARY KEY,
    ImportDate  DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Note        NVARCHAR(200),
    Status      NVARCHAR(30) NOT NULL DEFAULT N'Chưa hoàn thành'
                CHECK (Status IN (N'Chưa hoàn thành', N'Đã hoàn thành'))
);
GO

CREATE TABLE GOODS_RECEIPT_DETAIL (
    ReceiptID   INT NOT NULL,
    ProductID   INT NOT NULL,
    Quantity    INT NOT NULL CHECK (Quantity > 0),
    ImportPrice DECIMAL(18,2) NOT NULL CHECK (ImportPrice >= 0),
    Amount      AS (CAST(Quantity * ImportPrice AS DECIMAL(18,2))),
    PRIMARY KEY (ReceiptID, ProductID),
    FOREIGN KEY (ReceiptID) REFERENCES GOODS_RECEIPT(ReceiptID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID)
);
GO

-- =====================================================
-- 6. SALES_ORDER
-- =====================================================
CREATE TABLE SALES_ORDER (
    OrderID     INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID  INT NOT NULL,
    CreatedDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    DeliveryAddress NVARCHAR(200),
    OrderStatus NVARCHAR(30) NOT NULL DEFAULT N'Mới'
                CHECK (OrderStatus IN (N'Mới', N'Đang xử lý',
                                       N'Đã giao', N'Đã hủy')),
    FOREIGN KEY (CustomerID) REFERENCES CUSTOMER(CustomerID)
);
GO

CREATE TABLE ORDER_DETAIL (
    OrderID   INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity  INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    CostPrice DECIMAL(18,2) NOT NULL CHECK (CostPrice >= 0),
    Discount  DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (Discount >= 0),
    Amount    AS (CAST(Quantity * UnitPrice - Discount AS DECIMAL(18,2))),
    PRIMARY KEY (OrderID, ProductID),
    FOREIGN KEY (OrderID)   REFERENCES SALES_ORDER(OrderID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID)
);
GO

-- Bổ sung cho API: lưu giỏ hàng theo khách và địa chỉ giao hàng tại thời điểm đặt.
CREATE TABLE SHOPPING_CART_ITEM (
    CustomerID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (CustomerID, ProductID),
    FOREIGN KEY (CustomerID) REFERENCES CUSTOMER(CustomerID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID)
);
GO

-- =====================================================
-- 7. PAYMENT  (ĐÃ XÓA Amount - chuẩn 3NF)
-- =====================================================
CREATE TABLE PAYMENT (
    PaymentID   INT IDENTITY(1,1) PRIMARY KEY,
    OrderID     INT NOT NULL UNIQUE,
    Method      NVARCHAR(50) NOT NULL
                CHECK (Method IN (N'Tiền mặt', N'Chuyển khoản', N'Trực tuyến')),
    Status      NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán'
                CHECK (Status IN (N'Chưa thanh toán', N'Đã thanh toán')),
    PaymentDate DATE,
    FOREIGN KEY (OrderID) REFERENCES SALES_ORDER(OrderID)
);
GO

-- =====================================================
-- VIEW (TotalAmount / TotalPayment được tính từ chi tiết)
-- =====================================================
CREATE VIEW vw_OrderSummary AS
SELECT  o.OrderID, o.CustomerID, o.CreatedDate, o.OrderStatus,
        ISNULL(SUM(od.Quantity * od.UnitPrice),0)                       AS TotalProductAmount,
        ISNULL(SUM(od.Discount),0)                                      AS Discount,
        ISNULL(SUM(od.Quantity * od.UnitPrice - od.Discount),0)         AS TotalPayment,
        ISNULL(SUM((od.UnitPrice - od.CostPrice) * od.Quantity - od.Discount),0) AS Profit
FROM SALES_ORDER o
LEFT JOIN ORDER_DETAIL od ON od.OrderID = o.OrderID
GROUP BY o.OrderID, o.CustomerID, o.CreatedDate, o.OrderStatus;
GO

CREATE VIEW vw_GoodsReceiptSummary AS
SELECT  gr.ReceiptID, gr.ImportDate, gr.Note, gr.Status,
        ISNULL(SUM(grd.Amount), 0) AS TotalAmount
FROM GOODS_RECEIPT gr
LEFT JOIN GOODS_RECEIPT_DETAIL grd ON grd.ReceiptID = gr.ReceiptID
GROUP BY gr.ReceiptID, gr.ImportDate, gr.Note, gr.Status;
GO

CREATE VIEW vw_InventoryMovement AS
SELECT  p.ProductID, p.ProductName, p.ProductTypeID,
        ISNULL(g.TotalImport, 0) AS TotalImport,
        ISNULL(s.TotalExport, 0) AS TotalExport,
        p.StockQuantity          AS CurrentStock,
        p.MinStockLevel          AS MinStockLevel,
        CASE WHEN p.StockQuantity <= p.MinStockLevel
             THEN N'Cần nhập thêm' ELSE N'Đủ hàng' END AS StockWarning
FROM PRODUCT p
LEFT JOIN (SELECT ProductID, SUM(Quantity) AS TotalImport
           FROM GOODS_RECEIPT_DETAIL GROUP BY ProductID) g
       ON g.ProductID = p.ProductID
LEFT JOIN (SELECT ProductID, SUM(Quantity) AS TotalExport
           FROM ORDER_DETAIL GROUP BY ProductID) s
       ON s.ProductID = p.ProductID;
GO

CREATE VIEW vw_LowStockProducts AS
SELECT  p.ProductID, p.ProductName, p.StockQuantity,
        p.MinStockLevel, pt.ProductTypeName
FROM PRODUCT p
JOIN PRODUCT_TYPE pt ON pt.ProductTypeID = p.ProductTypeID
WHERE p.Status = 1 AND p.StockQuantity <= p.MinStockLevel;
GO

-- =====================================================
-- TRIGGER
-- =====================================================

-- -----------------------------------------------------
-- 1) Chi tiết phiếu nhập: kiểm tra giá/số lượng,
--    chặn sửa nếu phiếu đã hoàn thành.
--    (Không còn cập nhật TotalAmount vì đã bỏ cột đó)
-- -----------------------------------------------------
CREATE TRIGGER trg_GoodsReceiptDetail_Adjust
ON GOODS_RECEIPT_DETAIL
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1.1 Kiểm tra giá và số lượng nhập
    IF EXISTS (SELECT 1 FROM inserted WHERE Quantity <= 0 OR ImportPrice < 0)
    BEGIN
        RAISERROR (N'Số lượng nhập phải > 0 và đơn giá nhập phải >= 0.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 1.2 Chặn sửa chi tiết của phiếu đã hoàn thành
    IF EXISTS (
        SELECT 1
        FROM (SELECT ReceiptID FROM inserted
              UNION
              SELECT ReceiptID FROM deleted) a
        JOIN GOODS_RECEIPT gr ON gr.ReceiptID = a.ReceiptID
        WHERE gr.Status = N'Đã hoàn thành'
    )
    BEGIN
        RAISERROR (N'Phiếu nhập đã hoàn thành, không thể sửa chi tiết.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

-- -----------------------------------------------------
-- 2) Khi phiếu nhập chuyển sang Đã hoàn thành:
--    cộng kho + cập nhật giá vốn nếu có thay đổi.
-- -----------------------------------------------------
CREATE TRIGGER trg_GoodsReceipt_Complete
ON GOODS_RECEIPT
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(Status) RETURN;

    -- 2.1 Cộng kho cho các sản phẩm thuộc phiếu vừa hoàn thành
    UPDATE p
    SET p.StockQuantity = p.StockQuantity + x.TotalQty
    FROM PRODUCT p
    JOIN (
        SELECT grd.ProductID, SUM(grd.Quantity) AS TotalQty
        FROM GOODS_RECEIPT_DETAIL grd
        JOIN inserted i ON i.ReceiptID = grd.ReceiptID
        JOIN deleted  d ON d.ReceiptID = i.ReceiptID
        WHERE d.Status = N'Chưa hoàn thành'
          AND i.Status = N'Đã hoàn thành'
        GROUP BY grd.ProductID
    ) x ON x.ProductID = p.ProductID;

    -- 2.2 Giá vốn bình quân sau khi cộng kho
    UPDATE p
    SET p.CostPrice = CASE WHEN p.StockQuantity = 0
                           THEN p.CostPrice
                           ELSE ((p.StockQuantity - x.TotalQty) * p.CostPrice + x.TotalImportCost)
                                / NULLIF(p.StockQuantity, 0) END
    FROM PRODUCT p
    JOIN (
        SELECT grd.ProductID, SUM(grd.Quantity) AS TotalQty,
               SUM(grd.Quantity * grd.ImportPrice) AS TotalImportCost
        FROM GOODS_RECEIPT_DETAIL grd
        JOIN inserted i ON i.ReceiptID = grd.ReceiptID
        JOIN deleted  d ON d.ReceiptID = i.ReceiptID
        WHERE d.Status = N'Chưa hoàn thành'
          AND i.Status = N'Đã hoàn thành'
        GROUP BY grd.ProductID
    ) x ON x.ProductID = p.ProductID;
END
GO

-- -----------------------------------------------------
-- 3) Chặn update phiếu nhập đã hoàn thành
-- -----------------------------------------------------
CREATE TRIGGER trg_GoodsReceipt_BlockUpdate
ON GOODS_RECEIPT
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF (UPDATE(Status) OR UPDATE(ImportDate) OR UPDATE(Note))
       AND EXISTS (SELECT 1 FROM deleted WHERE Status = N'Đã hoàn thành')
    BEGIN
        RAISERROR (N'Phiếu nhập đã hoàn thành, không thể cập nhật.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END
GO

-- -----------------------------------------------------
-- 4) Kiểm tra tồn kho trước khi cho phép mua
-- -----------------------------------------------------
CREATE TRIGGER trg_OrderDetail_CheckStock
ON ORDER_DETAIL
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM PRODUCT p
        JOIN (
            SELECT ProductID, SUM(Quantity) AS NeedQty
            FROM inserted
            GROUP BY ProductID
        ) x ON x.ProductID = p.ProductID
        WHERE p.StockQuantity < x.NeedQty
    )
    BEGIN
        RAISERROR (N'Tồn kho không đủ để bán.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

-- -----------------------------------------------------
-- 5) Trừ kho khi in hóa đơn thành công (Mới -> Đang xử lý)
--    Hoàn kho khi hủy hóa đơn (Đang xử lý -> Đã hủy)
-- -----------------------------------------------------
CREATE TRIGGER trg_SalesOrder_Stock
ON SALES_ORDER
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(OrderStatus) RETURN;

    -- 5.1 Mới -> Đang xử lý: kiểm tra đủ hàng rồi trừ kho
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted d ON d.OrderID = i.OrderID
        WHERE d.OrderStatus = N'Mới' AND i.OrderStatus = N'Đang xử lý'
    )
    BEGIN
        IF EXISTS (
            SELECT 1 FROM PRODUCT p
            JOIN (
                SELECT od.ProductID, SUM(od.Quantity) AS NeedQty
                FROM ORDER_DETAIL od
                JOIN inserted i ON i.OrderID = od.OrderID
                JOIN deleted  d ON d.OrderID = i.OrderID
                WHERE d.OrderStatus = N'Mới'
                  AND i.OrderStatus = N'Đang xử lý'
                GROUP BY od.ProductID
            ) x ON x.ProductID = p.ProductID
            WHERE p.StockQuantity < x.NeedQty
        )
        BEGIN
            RAISERROR (N'Tồn kho không đủ để in hóa đơn.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE p
        SET p.StockQuantity = p.StockQuantity - x.NeedQty
        FROM PRODUCT p
        JOIN (
            SELECT od.ProductID, SUM(od.Quantity) AS NeedQty
            FROM ORDER_DETAIL od
            JOIN inserted i ON i.OrderID = od.OrderID
            JOIN deleted  d ON d.OrderID = i.OrderID
            WHERE d.OrderStatus = N'Mới'
              AND i.OrderStatus = N'Đang xử lý'
            GROUP BY od.ProductID
        ) x ON x.ProductID = p.ProductID;
    END

    -- 5.2 Đang xử lý -> Đã hủy: hoàn kho
    IF EXISTS (
        SELECT 1 FROM inserted i JOIN deleted d ON d.OrderID = i.OrderID
        WHERE d.OrderStatus = N'Đang xử lý' AND i.OrderStatus = N'Đã hủy'
    )
    BEGIN
        UPDATE p
        SET p.StockQuantity = p.StockQuantity + x.RestoreQty
        FROM PRODUCT p
        JOIN (
            SELECT od.ProductID, SUM(od.Quantity) AS RestoreQty
            FROM ORDER_DETAIL od
            JOIN inserted i ON i.OrderID = od.OrderID
            JOIN deleted  d ON d.OrderID = i.OrderID
            WHERE d.OrderStatus = N'Đang xử lý'
              AND i.OrderStatus = N'Đã hủy'
            GROUP BY od.ProductID
        ) x ON x.ProductID = p.ProductID;
    END
END
GO

-- -----------------------------------------------------
-- 6) Chặn cập nhật trạng thái đơn hàng quay lui
-- -----------------------------------------------------
CREATE TRIGGER trg_SalesOrder_BlockBackwardStatus
ON SALES_ORDER
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(OrderStatus) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted  d ON d.OrderID = i.OrderID
        WHERE NOT (
                (d.OrderStatus = N'Mới'
                    AND i.OrderStatus IN (N'Mới', N'Đang xử lý', N'Đã hủy'))
             OR (d.OrderStatus = N'Đang xử lý'
                    AND i.OrderStatus IN (N'Đang xử lý', N'Đã giao', N'Đã hủy'))
             OR (d.OrderStatus = N'Đã giao'   AND i.OrderStatus = N'Đã giao')
             OR (d.OrderStatus = N'Đã hủy'    AND i.OrderStatus = N'Đã hủy')
        )
    )
    BEGIN
        RAISERROR (N'Không thể chuyển trạng thái đơn hàng quay lui.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

-- =====================================================
-- DỮ LIỆU MẪU
-- =====================================================

-- ACCOUNT
-- Tài khoản admin mẫu và khách mẫu đều dùng mật khẩu HoaDemo@2026 (hash PBKDF2 tương thích API).
INSERT INTO ACCOUNT (Username, PasswordHash, Role, Status, CreatedDate) VALUES
('admin1','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'QuanTriVien',1,'2024-01-01'),
('admin2','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'QuanTriVien',1,'2024-01-02'),
('admin3','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'QuanTriVien',1,'2024-01-03'),
('user4', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-06'),
('user5', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-07'),
('user6', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-08'),
('user7', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-09'),
('user8', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-10'),
('user9', 'pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-11'),
('user10','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-12'),
('user11','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-13'),
('user12','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-14'),
('user13','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-15'),
('user14','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-16'),
('user15','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-17'),
('user16','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-18'),
('user17','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-19'),
('user18','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-20'),
('user19','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-21'),
('user20','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-22'),
('user21','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-23'),
('user22','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-24'),
('user23','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-25'),
('user24','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-26'),
('user25','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-27'),
('user26','pbkdf2-sha256$600000$U2hvcEFwaVNlZWRTYWx0MQ==$oDeBjJvaa4Pp4S3r75uRTmT6W9WI0vsbHd/M18+yob0=',N'KhachHang',1,'2024-01-28');
GO

-- CUSTOMER
INSERT INTO CUSTOMER (FullName, Gender, DateOfBirth, Email, PhoneNumber, Address,
                      RegistrationDate, AccountID) VALUES
(N'Nguyễn Văn An',  N'Nam','1995-03-12','kh1@example.com', '0900000001',N'Hà Nội',       '2024-01-06', 4),
(N'Trần Thị Bích',  N'Nữ', '1997-06-25','kh2@example.com', '0900000002',N'TP.HCM',       '2024-01-07', 5),
(N'Lê Hoàng Cường', N'Nam','1993-09-08','kh3@example.com', '0900000003',N'Đà Nẵng',      '2024-01-08', 6),
(N'Phạm Thị Dung',  N'Nữ', '1998-12-01','kh4@example.com', '0900000004',N'Hải Phòng',    '2024-01-09', 7),
(N'Hoàng Văn Em',   N'Nam','1990-02-14','kh5@example.com', '0900000005',N'Cần Thơ',      '2024-01-10', 8),
(N'Vũ Thị Phương',  N'Nữ', '1996-07-19','kh6@example.com', '0900000006',N'Huế',          '2024-01-11', 9),
(N'Đặng Văn Giang', N'Nam','1994-11-23','kh7@example.com', '0900000007',N'Nha Trang',    '2024-01-12',10),
(N'Bùi Thị Hoa',    N'Nữ', '1999-04-05','kh8@example.com', '0900000008',N'Vũng Tàu',     '2024-01-13',11),
(N'Ngô Văn Inh',    N'Nam','1992-08-17','kh9@example.com', '0900000009',N'Biên Hòa',     '2024-01-14',12),
(N'Đỗ Thị Kim',     N'Nữ', '2000-01-28','kh10@example.com','0900000010',N'Buôn Ma Thuột','2024-01-15',13),
(N'Lý Văn Long',    N'Nam','1991-05-09','kh11@example.com','0900000011',N'Pleiku',       '2024-01-16',14),
(N'Trịnh Thị Mai',  N'Nữ', '1997-10-30','kh12@example.com','0900000012',N'Quy Nhơn',     '2024-01-17',15),
(N'Phan Văn Nam',   N'Nam','1993-03-21','kh13@example.com','0900000013',N'Đà Lạt',       '2024-01-18',16),
(N'Dương Thị Oanh', N'Nữ', '1995-09-14','kh14@example.com','0900000014',N'Long Xuyên',   '2024-01-19',17),
(N'Lâm Văn Phúc',   N'Nam','1996-12-06','kh15@example.com','0900000015',N'Rạch Giá',     '2024-01-20',18),
(N'Võ Thị Quỳnh',   N'Nữ', '1998-02-27','kh16@example.com','0900000016',N'Cà Mau',       '2024-01-21',19),
(N'Đinh Văn Sơn',   N'Nam','1990-06-11','kh17@example.com','0900000017',N'Bắc Ninh',     '2024-01-22',20),
(N'Hoàng Thị Trang',N'Nữ', '1994-08-02','kh18@example.com','0900000018',N'Nam Định',     '2024-01-23',21),
(N'Nguyễn Văn Uy',  N'Nam','1991-10-19','kh19@example.com','0900000019',N'Thái Bình',    '2024-01-24',22),
(N'Trần Thị Vân',   N'Nữ', '1999-01-07','kh20@example.com','0900000020',N'Vinh',         '2024-01-25',23),
(N'Lê Văn Xuân',    N'Nam','1992-04-15','kh21@example.com','0900000021',N'Thanh Hóa',    '2024-01-26',24),
(N'Phạm Thị Yến',   N'Nữ', '1997-11-28','kh22@example.com','0900000022',N'Quảng Ninh',   '2024-01-27',25),
(N'Hoàng Văn Anh',  N'Nam','1995-07-03','kh23@example.com','0900000023',N'Lạng Sơn',     '2024-01-28',26);
GO

-- PRODUCT_TYPE
INSERT INTO PRODUCT_TYPE (ProductTypeName, Description, Status) VALUES
(N'Hoa Cẩm Tú',      N'Hoa cẩm tú cầu các loại',       1),
(N'Hoa Tulip',       N'Hoa tulip nhập khẩu',            1),
(N'Hoa hồng',        N'Hoa hồng các loại',              1),
(N'Hoa hướng dương', N'Hoa hướng dương',                1),
(N'Hoa Baby',        N'Hoa baby trang trí',             1),
(N'Hoa Đồng Tiền',   N'Hoa đồng tiền',                  1),
(N'Hoa ly',          N'Hoa ly',                         1),
(N'Lan Hồ Điệp',     N'Lan hồ điệp',                    1);
GO

-- PRODUCT
INSERT INTO PRODUCT (ProductName, ProductTypeID, Description, Color, Unit,
                     CostPrice, ProfitRate, StockQuantity, MinStockLevel, Image, Status) VALUES
-- Hoa Cẩm Tú (Type 1)
(N'Bó Cẩm Tú Cầu Mix Pastel',          1, N'Bó hoa pastel nhẹ nhàng',                                 N'Hồng', N'Bó',  280000, 25, 0, 5, N'camtu-pastel.jpg',      1),
(N'Bó Cẩm Tú Cầu Mix Hồng',            1, N'Bó Cẩm Tú Cầu sắc hồng lãng mạn, gói giấy cao cấp.',        N'Hồng', N'Bó',  304000, 25, 0, 5, N'camtu-hong.jpg',        1),
(N'Hộp Cẩm Tú Cầu Cao Cấp',            1, N'Hộp hoa Cẩm Tú Cầu cao cấp, thiết kế sang trọng.',         N'Hồng', N'Hộp', 600000, 25, 0, 5, N'camtu-hop-cao-cap.jpg', 1),
(N'Bó Cẩm Tú Cầu Trắng Thanh Lịch',    1, N'Bó hoa trắng tinh khôi, tối giản nhưng sang trọng.',       N'Trắng',N'Bó',  256000, 25, 0, 5, N'camtu-trang.jpg',       1),
(N'Bó Cẩm Tú Cầu Lavender',            1, N'Gam màu tím lavender nhẹ nhàng, dịu dàng, lãng mạn.',       N'Tím',  N'Bó',  288000, 25, 0, 5, N'camtu-lavender.jpg',    1),
(N'Lọ Cẩm Tú Cầu Mini',                1, N'Lọ hoa nhỏ xinh để bàn làm việc.',                         N'Hồng', N'Lọ',  144000, 25, 0, 5, N'camtu-mini.jpg',        1),
(N'Bó Cẩm Tú Cầu Mix Hồng Lạc Thần',   1, N'Gam màu chuyển nhẹ độc đáo tạo hiệu ứng nổi bật.',         N'Hồng', N'Bó',  336000, 25, 0, 5, N'camtu-ombre.jpg',       1),
(N'Bó Cẩm Tú Cầu Babyblue',            1, N'Sắc xanh baby dịu nhẹ, phong cách hiện đại.',              N'Xanh', N'Bó',  320000, 25, 0, 5, N'camtu-babyblue.jpg',    1),

-- Hoa Tulip (Type 2)
(N'Bó Tulip Đỏ Lãng Mạn',              2, N'Tulip đỏ tượng trưng tình yêu mãnh liệt.',                 N'Đỏ',   N'Bó',  336000, 25, 0, 5, N'tulip-do.jpg',          1),
(N'Bó Tulip Hồng Dịu Dàng',            2, N'Tulip hồng nhẹ nhàng, thích hợp tặng người thương.',       N'Hồng', N'Bó',  320000, 25, 0, 5, N'tulip-hong.png',        1),
(N'Bó Tulip Vàng Tươi Sáng',           2, N'Tulip vàng tượng trưng niềm vui và may mắn.',              N'Vàng', N'Bó',  312000, 25, 0, 5, N'tulip-vang.jpeg',       1),
(N'Bó Tulip Trắng Thanh Khiết',        2, N'Tulip trắng thanh lịch, chân thành và tinh khôi.',         N'Trắng',N'Bó',  328000, 25, 0, 5, N'tulip-trang.jpg',       1),
(N'Bó Tulip Tím Hoàng Gia',            2, N'Tulip tím sang trọng và độc đáo.',                          N'Tím',  N'Bó',  360000, 25, 0, 5, N'tulip-tim.png',         1),
(N'Lọ Tulip Mini',                     2, N'Lọ tulip mini nhỏ xinh trang trí bàn.',                     N'Hồng', N'Lọ',  176000, 25, 0, 5, N'tulip-mini.jpg',        1),
(N'Bó Tulip Mix Màu',                  2, N'Bó tulip mix nhiều màu sắc nổi bật và tươi tắn.',          N'Mix',  N'Bó',  384000, 25, 0, 5, N'tulip-mix.png',         1),

-- Hoa hồng (Type 3)
(N'Bó Hoa Hồng Đỏ Cổ Điển',            3, N'Hoa hồng đỏ cổ điển, tình yêu nồng cháy và lãng mạn.',     N'Đỏ',   N'Bó',  360000, 25, 0, 5, N'rose-red.jpg',          1),
(N'Bó Hoa Hồng Hồng Lãng Mạn',         3, N'Hoa hồng hồng nhẹ nhàng, yêu thương và dịu dàng.',          N'Hồng', N'Bó',  336000, 25, 0, 5, N'rose-pink.jpg',         1),
(N'Bó Hoa Hồng Trắng Tinh Khôi',       3, N'Hoa hồng trắng thuần khiết và chân thành.',                N'Trắng',N'Bó',  344000, 25, 0, 5, N'rose-white.jpg',        1),
(N'Bó Hoa Hồng Vàng Tươi Sáng',        3, N'Hoa hồng vàng tượng trưng niềm vui và tình bạn.',          N'Vàng', N'Bó',  320000, 25, 0, 5, N'rose-yellow.jpg',       1),
(N'Bó Hoa Hồng Xanh Độc Đáo',          3, N'Hoa hồng xanh huyền bí và sang trọng.',                    N'Xanh', N'Bó',  416000, 25, 0, 5, N'rose-blue.jpg',         1),
(N'Hộp Hoa Hồng Cao Cấp',              3, N'Hộp hoa hồng thiết kế sang trọng, làm quà tặng.',          N'Đỏ',   N'Hộp', 600000, 25, 0, 5, N'rose-box.jpg',          1),
(N'Lọ Hoa Hồng Mini',                  3, N'Lọ hoa hồng nhỏ xinh trang trí bàn làm việc.',             N'Hồng', N'Lọ',  176000, 25, 0, 5, N'rose-mini.jpg',         1),

-- Hoa hướng dương (Type 4)
(N'Bó Hướng Dương Rực Rỡ',             4, N'Hướng dương rực rỡ, lạc quan và năng lượng tích cực.',     N'Vàng', N'Bó',  280000, 25, 0, 5, N'sun1.jpg',              1),
(N'Bó Hướng Dương Nắng Sớm',           4, N'Hướng dương tươi sáng, ấm áp như nắng ban mai.',            N'Vàng', N'Bó',  296000, 25, 0, 5, N'sun2.jpg',              1),
(N'Hướng Dương Mix Hoa Trắng',         4, N'Kết hợp hướng dương và hoa trắng thanh lịch, tinh tế.',    N'Vàng', N'Bó',  336000, 25, 0, 5, N'sun3.jpg',              1),
(N'Hộp Hướng Dương Cao Cấp',           4, N'Hộp hướng dương cao cấp, quà tặng sang trọng.',             N'Vàng', N'Hộp', 496000, 25, 0, 5, N'sun4.jpg',              1),
(N'Hướng Dương Mix Baby',              4, N'Kết hợp ngọt ngào giữa hướng dương và hoa baby.',          N'Vàng', N'Bó',  320000, 25, 0, 5, N'sun5.jpg',              1),
(N'Bó Hướng Dương Vintage',            4, N'Hướng dương phong cách vintage nhẹ nhàng, tinh tế.',        N'Vàng', N'Bó',  304000, 25, 0, 5, N'sun6.jpg',              1),
(N'Lọ Hướng Dương Mini',               4, N'Lọ hướng dương nhỏ xinh trang trí bàn làm việc.',          N'Vàng', N'Lọ',  168000, 25, 0, 5, N'sun7.jpg',              1),
(N'Giỏ Hướng Dương Tươi',              4, N'Giỏ hướng dương tươi rực rỡ, năng lượng tích cực.',         N'Vàng', N'Giỏ', 384000, 25, 0, 5, N'sun8.jpg',              1),

-- Hoa Baby (Type 5)
(N'Bó Hoa Baby Trắng Tinh Khôi',       5, N'Baby trắng tinh khôi, nhẹ nhàng và thuần khiết.',           N'Trắng',N'Bó',  176000, 25, 0, 5, N'baby-trang.jpg',        1),
(N'Bó Hoa Baby Hồng Ngọt Ngào',        5, N'Baby hồng dịu dàng, tình yêu lãng mạn.',                    N'Hồng', N'Bó',  200000, 25, 0, 5, N'baby-hong.jpg',         1),
(N'Bó Hoa Baby Xanh Pastel',           5, N'Sắc xanh pastel hiện đại và thanh lịch.',                   N'Xanh', N'Bó',  208000, 25, 0, 5, N'baby-xanh.jpg',         1),
(N'Bó Hoa Baby Tím Lavender',          5, N'Gam màu tím lavender nhẹ nhàng và tinh tế.',                N'Tím',  N'Bó',  216000, 25, 0, 5, N'baby-tim.jpg',          1),
(N'Lọ Hoa Baby Mini',                  5, N'Lọ hoa nhỏ xinh trang trí bàn làm việc.',                   N'Trắng',N'Lọ',  120000, 25, 0, 5, N'baby-mini.jpg',         1),
(N'Bó Hoa Baby Mix Màu',               5, N'Baby nhiều màu sắc tươi sáng và nổi bật.',                  N'Mix',  N'Bó',  232000, 25, 0, 5, N'baby-mix.jpg',          1),
(N'Bó Hoa Baby Ombre',                 5, N'Hiệu ứng màu chuyển độc đáo tạo điểm nhấn nổi bật.',        N'Mix',  N'Bó',  336000, 25, 0, 5, N'camtu-ombre.jpg',       1),
(N'Hộp Hoa Baby Cao Cấp',              5, N'Hộp hoa baby sang trọng, thích hợp làm quà tặng.',          N'Trắng',N'Hộp', 384000, 25, 0, 5, N'baby-hop.jpg',          1);
GO

UPDATE PRODUCT SET ProductCode=N'HH'+RIGHT(N'00000'+CONVERT(NVARCHAR(5),ProductID),5) WHERE ProductCode IS NULL;
ALTER TABLE PRODUCT ALTER COLUMN ProductCode NVARCHAR(50) NOT NULL;
ALTER TABLE PRODUCT ADD CONSTRAINT UQ_PRODUCT_ProductCode UNIQUE(ProductCode);
GO

-- GOODS_RECEIPT (không còn TotalAmount)
INSERT INTO GOODS_RECEIPT (ImportDate, Note) VALUES
('2024-01-01',N'Phiếu nhập 1'),('2024-01-02',N'Phiếu nhập 2'),
('2024-01-03',N'Phiếu nhập 3'),('2024-01-04',N'Phiếu nhập 4'),
('2024-01-05',N'Phiếu nhập 5'),('2024-01-06',N'Phiếu nhập 6'),
('2024-01-07',N'Phiếu nhập 7'),('2024-01-08',N'Phiếu nhập 8'),
('2024-01-09',N'Phiếu nhập 9'),('2024-01-10',N'Phiếu nhập 10'),
('2024-01-11',N'Phiếu nhập 11'),('2024-01-12',N'Phiếu nhập 12'),
('2024-01-13',N'Phiếu nhập 13'),('2024-01-14',N'Phiếu nhập 14'),
('2024-01-15',N'Phiếu nhập 15'),('2024-01-16',N'Phiếu nhập 16'),
('2024-01-17',N'Phiếu nhập 17'),('2024-01-18',N'Phiếu nhập 18'),
('2024-01-19',N'Phiếu nhập 19'),('2024-01-20',N'Phiếu nhập 20'),
('2024-01-21',N'Phiếu nhập 21'),('2024-01-22',N'Phiếu nhập 22'),
('2024-01-23',N'Phiếu nhập 23'),('2024-01-24',N'Phiếu nhập 24'),
('2024-01-25',N'Phiếu nhập 25');
GO

-- GOODS_RECEIPT_DETAIL (ImportPrice = CostPrice của từng sản phẩm)
INSERT INTO GOODS_RECEIPT_DETAIL (ReceiptID, ProductID, Quantity, ImportPrice) VALUES
(1, 1, 10, 280000),(1, 2,  8, 304000),
(2, 3,  5, 600000),(2, 4, 10, 256000),
(3, 5, 10, 288000),(3, 6, 15, 144000),
(4, 7,  8, 336000),(4, 8, 10, 320000),
(5, 9, 10, 336000),(5,10, 10, 320000),
(6,11, 10, 312000),(6,12,  8, 328000),
(7,13,  5, 360000),(7,14, 15, 176000),
(8,15,  8, 384000),(8,16, 10, 360000),
(9,17, 10, 336000),(9,18,  8, 344000),
(10,19,10, 320000),(10,20, 5, 416000),
(11,21, 5, 600000),(11,22,15, 176000),
(12,23,10, 280000),(12,24,10, 296000),
(13,25, 8, 336000),(13,26, 5, 496000),
(14,27,10, 320000),
(15,28,10, 304000),
(16,29,15, 168000),
(17,30, 8, 384000),
(18,31,15, 176000),
(19,32,10, 200000),
(20,33,10, 208000),
(21,34,10, 216000),
(22,35,20, 120000),
(23,36,10, 232000),
(24,37, 8, 336000),
(25,38, 8, 384000);
GO

-- Chuyển tất cả phiếu nhập sang Đã hoàn thành -> trigger cộng kho + cập nhật giá vốn
UPDATE GOODS_RECEIPT SET Status = N'Đã hoàn thành';
GO

-- SALES_ORDER (tạo ban đầu ở trạng thái Mới)
INSERT INTO SALES_ORDER (CustomerID, CreatedDate, OrderStatus) VALUES
(1,'2024-01-06',N'Mới'),(2,'2024-01-07',N'Mới'),(3,'2024-01-08',N'Mới'),
(4,'2024-01-09',N'Mới'),(5,'2024-01-10',N'Mới'),(6,'2024-01-11',N'Mới'),
(7,'2024-01-12',N'Mới'),(8,'2024-01-13',N'Mới'),(9,'2024-01-14',N'Mới'),
(10,'2024-01-15',N'Mới'),(11,'2024-01-16',N'Mới'),(12,'2024-01-17',N'Mới'),
(13,'2024-01-18',N'Mới'),(14,'2024-01-19',N'Mới'),(15,'2024-01-20',N'Mới'),
(16,'2024-01-21',N'Mới'),(17,'2024-01-22',N'Mới'),(18,'2024-01-23',N'Mới'),
(19,'2024-01-24',N'Mới'),(20,'2024-01-25',N'Mới'),(21,'2024-01-26',N'Mới'),
(22,'2024-01-27',N'Mới'),(23,'2024-01-28',N'Mới');
GO

-- ORDER_DETAIL
-- ORDER_DETAIL (UnitPrice = SellingPrice = CostPrice × 1.25)
INSERT INTO ORDER_DETAIL (OrderID, ProductID, Quantity, UnitPrice, CostPrice, Discount) VALUES
( 1, 1, 2, 350000, 280000, 0),
( 2, 2, 3, 380000, 304000, 0),
( 3, 3, 1, 750000, 600000, 0),
( 4, 4, 2, 320000, 256000, 0),
( 5, 5, 2, 360000, 288000, 0),
( 6, 6, 3, 180000, 144000, 0),
( 7, 7, 2, 420000, 336000, 0),
( 8, 8, 2, 400000, 320000, 0),
( 9, 9, 2, 420000, 336000, 0),
(10,10, 2, 400000, 320000, 0),
(11,11, 2, 390000, 312000, 0),
(12,12, 2, 410000, 328000, 0),
(13,13, 1, 450000, 360000, 0),
(14,14, 3, 220000, 176000, 0),
(15,15, 1, 480000, 384000, 0),
(16,16, 2, 450000, 360000, 0),
(17,17, 2, 420000, 336000, 0),
(18,18, 2, 430000, 344000, 0),
(19,19, 2, 400000, 320000, 0),
(20,20, 1, 520000, 416000, 0),
(21,21, 1, 750000, 600000, 0),
(22,22, 3, 220000, 176000, 0),
(23,23, 2, 350000, 280000, 0);
GO
-- PAYMENT (ĐÃ BỎ cột Amount - chỉ lưu thông tin thanh toán)
INSERT INTO PAYMENT (OrderID, Method, Status, PaymentDate) VALUES
(1, N'Tiền mặt',     N'Chưa thanh toán', NULL),
(2, N'Chuyển khoản', N'Đã thanh toán',   '2024-01-07'),
(3, N'Tiền mặt',     N'Chưa thanh toán', NULL),
(4, N'Chuyển khoản', N'Đã thanh toán',   '2024-01-09'),
(5, N'Tiền mặt',     N'Chưa thanh toán', NULL),
(6, N'Chuyển khoản', N'Đã thanh toán',   '2024-01-11'),
(7, N'Tiền mặt',     N'Chưa thanh toán', NULL),
(8, N'Chuyển khoản', N'Đã thanh toán',   '2024-01-13'),
(9, N'Tiền mặt',     N'Chưa thanh toán', NULL),
(10,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-15'),
(11,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(12,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-17'),
(13,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(14,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-19'),
(15,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(16,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-21'),
(17,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(18,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-23'),
(19,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(20,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-25'),
(21,N'Tiền mặt',     N'Chưa thanh toán', NULL),
(22,N'Chuyển khoản', N'Đã thanh toán',   '2024-01-27'),
(23,N'Tiền mặt',     N'Chưa thanh toán', NULL);
GO
