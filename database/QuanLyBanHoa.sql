-- =====================================================
-- QuanLyBanHoa - BẢN SỬA THEO KIỂM TRA 3NF & ĐỐI CHIẾU CHƯƠNG 3
-- =====================================================
USE master;
GO
IF DB_ID('QuanLyBanHoa') IS NOT NULL DROP DATABASE QuanLyBanHoa;
GO
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
                 CHECK (Role IN (N'KhachHang', N'QuanTriVien',
                                 N'NhanVienBanHang', N'NhanVienKho')),
    Status       BIT  NOT NULL DEFAULT 1,
    CreatedDate  DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);

-- =====================================================
-- 2. EMPLOYEE
-- =====================================================
CREATE TABLE EMPLOYEE (
    EmployeeID   INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(100) NOT NULL,
    Gender       NVARCHAR(10)  CHECK (Gender IN (N'Nam', N'Nữ', N'Khác')),
    DateOfBirth  DATE,
    Email        VARCHAR(100) UNIQUE,
    PhoneNumber  VARCHAR(15)  UNIQUE,
    Address      NVARCHAR(200),
    Position     NVARCHAR(50)  NOT NULL,
    HireDate     DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    AccountID    INT  NOT NULL UNIQUE,
    FOREIGN KEY (AccountID) REFERENCES ACCOUNT(AccountID)
);

-- =====================================================
-- 3. CUSTOMER_TYPE
-- =====================================================
CREATE TABLE CUSTOMER_TYPE (
    CustomerTypeID       INT IDENTITY(1,1) PRIMARY KEY,
    TypeName             NVARCHAR(50) NOT NULL UNIQUE,
    MinimumLoyaltyPoints INT NOT NULL CHECK (MinimumLoyaltyPoints >= 0),
    DiscountPercent      DECIMAL(5,2) NOT NULL CHECK (DiscountPercent BETWEEN 0 AND 100)
);

-- =====================================================
-- 4. CUSTOMER
-- =====================================================
CREATE TABLE CUSTOMER (
    CustomerID       INT IDENTITY(1,1) PRIMARY KEY,
    FullName         NVARCHAR(100) NOT NULL,
    Gender           NVARCHAR(10)  CHECK (Gender IN (N'Nam', N'Nữ', N'Khác')),
    DateOfBirth      DATE,
    Email            VARCHAR(100) UNIQUE,
    PhoneNumber      VARCHAR(15)  UNIQUE,
    Address          NVARCHAR(200),
    LoyaltyPoints    INT  NOT NULL DEFAULT 0 CHECK (LoyaltyPoints >= 0),
    RegistrationDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    AccountID        INT  NOT NULL UNIQUE,
    FOREIGN KEY (AccountID) REFERENCES ACCOUNT(AccountID)
);

-- =====================================================
-- 5. PRODUCT_TYPE
-- =====================================================
CREATE TABLE PRODUCT_TYPE (
    ProductTypeID   INT IDENTITY(1,1) PRIMARY KEY,
    ProductTypeName NVARCHAR(50) NOT NULL UNIQUE,
    Description     NVARCHAR(200),
    Status          BIT NOT NULL DEFAULT 1
);

-- =====================================================
-- 6. FLOWER
-- =====================================================
CREATE TABLE FLOWER (
    FlowerID    INT IDENTITY(1,1) PRIMARY KEY,
    FlowerName  NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(200)
);

-- =====================================================
-- 7. PRODUCT
--    [FIX] Thêm MinStockLevel để đáp ứng UC-ADM-08.2
-- =====================================================
CREATE TABLE PRODUCT (
    ProductID     INT IDENTITY(1,1) PRIMARY KEY,
    ProductName   NVARCHAR(100) NOT NULL,
    ProductTypeID INT NOT NULL,
    Description   NVARCHAR(500),
    Color         NVARCHAR(50),
    Unit          NVARCHAR(20)  NOT NULL,
    CostPrice     DECIMAL(18,2) NOT NULL CHECK (CostPrice >= 0),
    ProfitRate    DECIMAL(5,2)  NOT NULL CHECK (ProfitRate >= 0),
    SellingPrice  AS (CAST(CostPrice * (1 + ProfitRate/100.0) AS DECIMAL(18,2))),
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    MinStockLevel INT NOT NULL DEFAULT 5 CHECK (MinStockLevel >= 0),  -- [FIX]
    Image         NVARCHAR(255),
    Status        BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (ProductTypeID) REFERENCES PRODUCT_TYPE(ProductTypeID)
);

CREATE TABLE PRODUCT_COMPONENT (
    ProductID INT NOT NULL,
    FlowerID  INT NOT NULL,
    Quantity  INT NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (ProductID, FlowerID),
    FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID) ON DELETE CASCADE,
    FOREIGN KEY (FlowerID)  REFERENCES FLOWER(FlowerID)
);

-- =====================================================
-- 8. GOODS_RECEIPT
-- =====================================================
CREATE TABLE GOODS_RECEIPT (
    ReceiptID  INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeID INT  NOT NULL,
    ImportDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Note       NVARCHAR(200),
    FOREIGN KEY (EmployeeID) REFERENCES EMPLOYEE(EmployeeID)
);

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

-- =====================================================
-- 9. CART + CART_DETAIL
-- =====================================================
CREATE TABLE CART (
    CartID      INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID  INT NOT NULL,
    CreatedDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    UpdatedDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    FOREIGN KEY (CustomerID) REFERENCES CUSTOMER(CustomerID)
);

CREATE TABLE CART_DETAIL (
    CartID    INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity  INT NOT NULL CHECK (Quantity > 0),
    AddedDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    PRIMARY KEY (CartID, ProductID),
    FOREIGN KEY (CartID)    REFERENCES CART(CartID) ON DELETE CASCADE,
    FOREIGN KEY (ProductID) REFERENCES PRODUCT(ProductID)
);

-- =====================================================
-- 10. SALES_ORDER
--    [FIX] Bổ sung N'Đã giao' theo UC-ADM-07.1
-- =====================================================
CREATE TABLE SALES_ORDER (
    OrderID     INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID  INT NOT NULL,
    EmployeeID  INT NULL,
    CreatedDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    OrderStatus NVARCHAR(30) NOT NULL DEFAULT N'Mới'
                CHECK (OrderStatus IN (N'Mới', N'Đang xử lý',
                                       N'Đã giao', N'Đã hủy')),   -- [FIX]
    FOREIGN KEY (CustomerID) REFERENCES CUSTOMER(CustomerID),
    FOREIGN KEY (EmployeeID) REFERENCES EMPLOYEE(EmployeeID)
);

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

-- =====================================================
-- 11. PAYMENT
--    [FIX] N'Trực tiếp' → N'Tiền mặt' cho khớp UC-CUS-11
-- =====================================================
CREATE TABLE PAYMENT (
    PaymentID   INT IDENTITY(1,1) PRIMARY KEY,
    OrderID     INT NOT NULL UNIQUE,
    Method      NVARCHAR(50) NOT NULL
                CHECK (Method IN (N'Tiền mặt', N'Chuyển khoản')),  -- [FIX]
    Amount      DECIMAL(18,2) NOT NULL CHECK (Amount >= 0),
    Status      NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán'
                CHECK (Status IN (N'Chưa thanh toán', N'Đã thanh toán')),
    PaymentDate DATE,
    FOREIGN KEY (OrderID) REFERENCES SALES_ORDER(OrderID)
);

-- =====================================================
-- 12. DELIVERY
-- =====================================================
CREATE TABLE DELIVERY (
    DeliveryID      INT IDENTITY(1,1) PRIMARY KEY,
    OrderID         INT NOT NULL UNIQUE,
    ReceiverName    NVARCHAR(100) NOT NULL,
    ReceiverPhone   VARCHAR(15)   NOT NULL,
    DeliveryAddress NVARCHAR(200) NOT NULL,
    DeliveryStatus  NVARCHAR(30)  NOT NULL DEFAULT N'Chờ giao'
                    CHECK (DeliveryStatus IN (N'Chờ giao', N'Đang giao',
                                              N'Đã giao', N'Giao thất bại')),
    FOREIGN KEY (OrderID) REFERENCES SALES_ORDER(OrderID)
);
GO

-- =====================================================
-- VIEW
-- =====================================================
-- 1) Hạng khách suy ra từ LoyaltyPoints
CREATE VIEW vw_CustomerWithType AS
SELECT  c.CustomerID, c.FullName, c.LoyaltyPoints, c.RegistrationDate,
        ct.CustomerTypeID, ct.TypeName, ct.DiscountPercent
FROM CUSTOMER c
JOIN CUSTOMER_TYPE ct
  ON ct.MinimumLoyaltyPoints = (
       SELECT MAX(MinimumLoyaltyPoints)
       FROM CUSTOMER_TYPE ct2
       WHERE ct2.MinimumLoyaltyPoints <= c.LoyaltyPoints);
GO

-- 2) Tổng hợp đơn hàng
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

-- 3) Tổng tiền phiếu nhập
CREATE VIEW vw_GoodsReceiptSummary AS
SELECT  gr.ReceiptID, gr.EmployeeID, gr.ImportDate, gr.Note,
        ISNULL(SUM(grd.Amount),0) AS TotalAmount
FROM GOODS_RECEIPT gr
LEFT JOIN GOODS_RECEIPT_DETAIL grd ON grd.ReceiptID = gr.ReceiptID
GROUP BY gr.ReceiptID, gr.EmployeeID, gr.ImportDate, gr.Note;
GO

-- 4) [FIX] View nhập – xuất – tồn cho UC-ADM-08.3
CREATE VIEW vw_InventoryMovement AS
SELECT  p.ProductID,
        p.ProductName,
        p.ProductTypeID,
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

-- 5) [FIX] View cảnh báo sản phẩm sắp hết (UC-ADM-08.2)
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

-- 1) Cập nhật tồn kho khi nhập hàng
CREATE TRIGGER trg_GoodsReceiptDetail_Update
ON GOODS_RECEIPT_DETAIL
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE P
    SET P.StockQuantity = P.StockQuantity - x.TotalQty
    FROM PRODUCT P
    JOIN (SELECT ProductID, SUM(Quantity) AS TotalQty
          FROM deleted GROUP BY ProductID) x
      ON x.ProductID = P.ProductID;

    UPDATE P
    SET P.StockQuantity = P.StockQuantity + x.TotalQty
    FROM PRODUCT P
    JOIN (SELECT ProductID, SUM(Quantity) AS TotalQty
          FROM inserted GROUP BY ProductID) x
      ON x.ProductID = P.ProductID;
END;
GO

-- 2) Trừ tồn kho khi bán hàng + chặn bán vượt tồn
CREATE TRIGGER trg_OrderDetail_Stock
ON ORDER_DETAIL
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE P
    SET P.StockQuantity = P.StockQuantity + x.TotalQty
    FROM PRODUCT P
    JOIN (SELECT ProductID, SUM(Quantity) AS TotalQty
          FROM deleted GROUP BY ProductID) x
      ON x.ProductID = P.ProductID;

    UPDATE P
    SET P.StockQuantity = P.StockQuantity - x.TotalQty
    FROM PRODUCT P
    JOIN (SELECT ProductID, SUM(Quantity) AS TotalQty
          FROM inserted GROUP BY ProductID) x
      ON x.ProductID = P.ProductID;

    IF EXISTS (SELECT 1 FROM PRODUCT WHERE StockQuantity < 0)
    BEGIN
        RAISERROR (N'Tồn kho không đủ để bán.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 3) Giảm giá theo hạng khách hàng khi chèn ORDER_DETAIL
CREATE TRIGGER trg_OrderDetail_ApplyLoyaltyDiscount
ON ORDER_DETAIL
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE od
    SET Discount = CAST(od.Quantity * od.UnitPrice * ct.DiscountPercent / 100.0
                        AS DECIMAL(18,2))
    FROM ORDER_DETAIL od
    JOIN inserted i ON i.OrderID = od.OrderID AND i.ProductID = od.ProductID
    JOIN SALES_ORDER o ON o.OrderID = od.OrderID
    JOIN vw_CustomerWithType ct ON ct.CustomerID = o.CustomerID
    WHERE od.Discount = 0;
END;
GO

-- 4) Cập nhật UpdatedDate giỏ hàng
CREATE TRIGGER trg_CartDetail_Update
ON CART_DETAIL
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE C
    SET UpdatedDate = CAST(GETDATE() AS DATE)
    FROM CART C
    WHERE C.CartID IN (SELECT CartID FROM inserted
                       UNION SELECT CartID FROM deleted);
END;
GO

-- 5) [FIX] Chặn cập nhật trạng thái đơn hàng quay lui (UC-ADM-07.1 – E1)
-- 5) [FIX] Chặn cập nhật trạng thái đơn hàng quay lui (UC-ADM-07.1 – E1)
IF OBJECT_ID('trg_SalesOrder_BlockBackwardStatus','TR') IS NOT NULL
    DROP TRIGGER trg_SalesOrder_BlockBackwardStatus;
GO
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
             OR (d.OrderStatus = N'Đã giao'
                    AND i.OrderStatus = N'Đã giao')
             OR (d.OrderStatus = N'Đã hủy'
                    AND i.OrderStatus = N'Đã hủy')
        )
    )
    BEGIN
        RAISERROR (N'Không thể chuyển trạng thái đơn hàng quay lui.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

-- 6) [FIX] Chặn số lượng giỏ hàng vượt tồn kho (UC-CUS-09 – E2)
CREATE TRIGGER trg_CartDetail_CheckStock
ON CART_DETAIL
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN PRODUCT p ON p.ProductID = i.ProductID
        WHERE i.Quantity > p.StockQuantity
    )
    BEGIN
        RAISERROR (N'Số lượng mua vượt quá tồn kho khả dụng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 7) [FIX] Tự động đồng bộ trạng thái đơn khi DELIVERY đã giao (tùy chọn)
--    Đảm bảo SALES_ORDER.OrderStatus = N'Đã giao' khi DELIVERY.DeliveryStatus = N'Đã giao'
CREATE TRIGGER trg_Delivery_SyncOrderStatus
ON DELIVERY
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(DeliveryStatus) RETURN;

    UPDATE o
    SET o.OrderStatus = N'Đã giao'
    FROM SALES_ORDER o
    JOIN inserted i ON i.OrderID = o.OrderID
    WHERE i.DeliveryStatus = N'Đã giao'
      AND o.OrderStatus IN (N'Mới', N'Đang xử lý');  -- chỉ đẩy tới, không quay lui
END;
GO

-- =====================================================
-- DỮ LIỆU MẪU
-- =====================================================

-- ACCOUNT
INSERT INTO ACCOUNT (Username, PasswordHash, Role, Status, CreatedDate) VALUES
('admin1','HASH_pass1',N'QuanTriVien',1,'2024-01-01'),
('admin2','HASH_pass2',N'QuanTriVien',1,'2024-01-02'),
('admin3','HASH_pass3',N'QuanTriVien',1,'2024-01-03'),
('emp01', 'HASH_emp01',N'NhanVienBanHang',1,'2024-01-04'),
('emp02', 'HASH_emp02',N'NhanVienKho',    1,'2024-01-05'),
('user4', 'HASH_pass4',N'KhachHang',1,'2024-01-06'),
('user5', 'HASH_pass5',N'KhachHang',1,'2024-01-07'),
('user6', 'HASH_pass6',N'KhachHang',1,'2024-01-08'),
('user7', 'HASH_pass7',N'KhachHang',1,'2024-01-09'),
('user8', 'HASH_pass8',N'KhachHang',1,'2024-01-10'),
('user9', 'HASH_pass9',N'KhachHang',1,'2024-01-11'),
('user10','HASH_pass10',N'KhachHang',1,'2024-01-12'),
('user11','HASH_pass11',N'KhachHang',1,'2024-01-13'),
('user12','HASH_pass12',N'KhachHang',1,'2024-01-14'),
('user13','HASH_pass13',N'KhachHang',1,'2024-01-15'),
('user14','HASH_pass14',N'KhachHang',1,'2024-01-16'),
('user15','HASH_pass15',N'KhachHang',1,'2024-01-17'),
('user16','HASH_pass16',N'KhachHang',1,'2024-01-18'),
('user17','HASH_pass17',N'KhachHang',1,'2024-01-19'),
('user18','HASH_pass18',N'KhachHang',1,'2024-01-20'),
('user19','HASH_pass19',N'KhachHang',1,'2024-01-21'),
('user20','HASH_pass20',N'KhachHang',1,'2024-01-22'),
('user21','HASH_pass21',N'KhachHang',1,'2024-01-23'),
('user22','HASH_pass22',N'KhachHang',1,'2024-01-24'),
('user23','HASH_pass23',N'KhachHang',1,'2024-01-25'),
('user24','HASH_pass24',N'KhachHang',1,'2024-01-26'),
('user25','HASH_pass25',N'KhachHang',1,'2024-01-27'),
('user26','HASH_pass26',N'KhachHang',1,'2024-01-28');
GO

-- EMPLOYEE
INSERT INTO EMPLOYEE (FullName, Gender, DateOfBirth, Email, PhoneNumber, Address, Position, HireDate, AccountID) VALUES
(N'Trần Văn Bán',  N'Nam', '1990-01-10', 'emp01@example.com', '0880000001', N'Hà Nội', N'Nhân viên bán hàng', '2023-01-01', 4),
(N'Lê Thị Kho',    N'Nữ',  '1992-05-20', 'emp02@example.com', '0880000002', N'Hà Nội', N'Nhân viên kho',      '2023-02-01', 5);
GO

-- CUSTOMER_TYPE
INSERT INTO CUSTOMER_TYPE (TypeName, MinimumLoyaltyPoints, DiscountPercent) VALUES
(N'Thường',    0,    0),
(N'Bạc',       100,  5),
(N'Vàng',      500,  10),
(N'Kim Cương', 1000, 15),
(N'Bạch Kim',  2000, 20);
GO

-- CUSTOMER
INSERT INTO CUSTOMER (FullName, Gender, DateOfBirth, Email, PhoneNumber, Address,
                      LoyaltyPoints, RegistrationDate, AccountID) VALUES
(N'Nguyễn Văn An',  N'Nam','1995-03-12','kh1@example.com', '0900000001',N'Hà Nội',     10, '2024-01-06', 6),
(N'Trần Thị Bích',  N'Nữ', '1997-06-25','kh2@example.com', '0900000002',N'TP.HCM',     20, '2024-01-07', 7),
(N'Lê Hoàng Cường', N'Nam','1993-09-08','kh3@example.com', '0900000003',N'Đà Nẵng',    30, '2024-01-08', 8),
(N'Phạm Thị Dung',  N'Nữ', '1998-12-01','kh4@example.com', '0900000004',N'Hải Phòng',  40, '2024-01-09', 9),
(N'Hoàng Văn Em',   N'Nam','1990-02-14','kh5@example.com', '0900000005',N'Cần Thơ',   150, '2024-01-10',10),
(N'Vũ Thị Phương',  N'Nữ', '1996-07-19','kh6@example.com', '0900000006',N'Huế',       160, '2024-01-11',11),
(N'Đặng Văn Giang', N'Nam','1994-11-23','kh7@example.com', '0900000007',N'Nha Trang', 180, '2024-01-12',12),
(N'Bùi Thị Hoa',    N'Nữ', '1999-04-05','kh8@example.com', '0900000008',N'Vũng Tàu',  200, '2024-01-13',13),
(N'Ngô Văn Inh',    N'Nam','1992-08-17','kh9@example.com', '0900000009',N'Biên Hòa',  250, '2024-01-14',14),
(N'Đỗ Thị Kim',     N'Nữ', '2000-01-28','kh10@example.com','0900000010',N'Buôn Ma Thuột',300,'2024-01-15',15),
(N'Lý Văn Long',    N'Nam','1991-05-09','kh11@example.com','0900000011',N'Pleiku',    350, '2024-01-16',16),
(N'Trịnh Thị Mai',  N'Nữ', '1997-10-30','kh12@example.com','0900000012',N'Quy Nhơn',  400, '2024-01-17',17),
(N'Phan Văn Nam',   N'Nam','1993-03-21','kh13@example.com','0900000013',N'Đà Lạt',    500, '2024-01-18',18),
(N'Dương Thị Oanh', N'Nữ', '1995-09-14','kh14@example.com','0900000014',N'Long Xuyên',520, '2024-01-19',19),
(N'Lâm Văn Phúc',   N'Nam','1996-12-06','kh15@example.com','0900000015',N'Rạch Giá',  540, '2024-01-20',20),
(N'Võ Thị Quỳnh',   N'Nữ', '1998-02-27','kh16@example.com','0900000016',N'Cà Mau',    560, '2024-01-21',21),
(N'Đinh Văn Sơn',   N'Nam','1990-06-11','kh17@example.com','0900000017',N'Bắc Ninh',  580, '2024-01-22',22),
(N'Hoàng Thị Trang',N'Nữ', '1994-08-02','kh18@example.com','0900000018',N'Nam Định',  600, '2024-01-23',23),
(N'Nguyễn Văn Uy',  N'Nam','1991-10-19','kh19@example.com','0900000019',N'Thái Bình', 620, '2024-01-24',24),
(N'Trần Thị Vân',   N'Nữ', '1999-01-07','kh20@example.com','0900000020',N'Vinh',     1000, '2024-01-25',25),
(N'Lê Văn Xuân',    N'Nam','1992-04-15','kh21@example.com','0900000021',N'Thanh Hóa',1050, '2024-01-26',26),
(N'Phạm Thị Yến',   N'Nữ', '1997-11-28','kh22@example.com','0900000022',N'Quảng Ninh',1100,'2024-01-27',27),
(N'Hoàng Văn Anh',  N'Nam','1995-07-03','kh23@example.com','0900000023',N'Lạng Sơn', 1150, '2024-01-28',28);
GO

-- PRODUCT_TYPE
INSERT INTO PRODUCT_TYPE (ProductTypeName, Description, Status) VALUES
(N'Hoa hồng',       N'Hoa hồng các loại',   1),
(N'Hoa lan',        N'Hoa lan hồ điệp',     1),
(N'Hoa tulip',      N'Hoa tulip nhập khẩu', 1),
(N'Hoa cẩm tú cầu', N'Hoa cẩm tú cầu',      1),
(N'Hoa hướng dương',N'Hoa hướng dương',     1);
GO

-- FLOWER
INSERT INTO FLOWER (FlowerName, Description) VALUES
(N'Hoa hồng',        N'Hoa hồng'),
(N'Hoa lan',         N'Hoa lan hồ điệp'),
(N'Hoa tulip',       N'Hoa tulip'),
(N'Hoa cẩm tú cầu',  N'Hoa cẩm tú cầu'),
(N'Hoa hướng dương', N'Hoa hướng dương'),
(N'Lá dương xỉ',     N'Phụ liệu trang trí'),
(N'Hoa baby',        N'Hoa baby trắng');
GO

-- PRODUCT (bổ sung MinStockLevel)
INSERT INTO PRODUCT (ProductName, ProductTypeID, Description, Color, Unit,
                     CostPrice, ProfitRate, StockQuantity, MinStockLevel, Image, Status) VALUES
(N'Hoa hồng đỏ 1',      1,N'Mô tả SP 1', N'Đỏ',   N'Bó', 51000,11,0,5,'hinhanh1.jpg',1),
(N'Hoa hồng hồng 2',    1,N'Mô tả SP 2', N'Hồng', N'Hộp',52000,12,0,5,'hinhanh2.jpg',1),
(N'Hoa hồng trắng 3',   1,N'Mô tả SP 3', N'Trắng',N'Giỏ',53000,13,0,5,'hinhanh3.jpg',1),
(N'Hoa hồng vàng 4',    1,N'Mô tả SP 4', N'Vàng', N'Bó', 54000,14,0,5,'hinhanh4.jpg',1),
(N'Hoa lan đỏ 5',       2,N'Mô tả SP 5', N'Đỏ',   N'Hộp',55000,15,0,5,'hinhanh5.jpg',1),
(N'Hoa lan hồng 6',     2,N'Mô tả SP 6', N'Hồng', N'Giỏ',56000,16,0,5,'hinhanh6.jpg',1),
(N'Hoa lan trắng 7',    2,N'Mô tả SP 7', N'Trắng',N'Bó', 57000,17,0,5,'hinhanh7.jpg',1),
(N'Hoa lan vàng 8',     2,N'Mô tả SP 8', N'Vàng', N'Hộp',58000,18,0,5,'hinhanh8.jpg',1),
(N'Hoa tulip đỏ 9',     3,N'Mô tả SP 9', N'Đỏ',   N'Giỏ',59000,19,0,5,'hinhanh9.jpg',1),
(N'Hoa tulip hồng 10',  3,N'Mô tả SP 10',N'Hồng', N'Bó', 60000,20,0,5,'hinhanh10.jpg',1),
(N'Hoa tulip trắng 11', 3,N'Mô tả SP 11',N'Trắng',N'Hộp',61000,21,0,5,'hinhanh11.jpg',1),
(N'Hoa tulip vàng 12',  3,N'Mô tả SP 12',N'Vàng', N'Giỏ',62000,22,0,5,'hinhanh12.jpg',1),
(N'Hoa cẩm tú cầu đỏ 13',   4,N'Mô tả SP 13',N'Đỏ',   N'Bó', 63000,23,0,5,'hinhanh13.jpg',1),
(N'Hoa cẩm tú cầu hồng 14', 4,N'Mô tả SP 14',N'Hồng', N'Hộp',64000,24,0,5,'hinhanh14.jpg',1),
(N'Hoa cẩm tú cầu trắng 15',4,N'Mô tả SP 15',N'Trắng',N'Giỏ',65000,25,0,5,'hinhanh15.jpg',1),
(N'Hoa cẩm tú cầu vàng 16', 4,N'Mô tả SP 16',N'Vàng', N'Bó', 66000,26,0,5,'hinhanh16.jpg',1),
(N'Hoa hướng dương đỏ 17',  5,N'Mô tả SP 17',N'Đỏ',   N'Hộp',67000,27,0,5,'hinhanh17.jpg',1),
(N'Hoa hướng dương hồng 18',5,N'Mô tả SP 18',N'Hồng', N'Giỏ',68000,28,0,5,'hinhanh18.jpg',1),
(N'Hoa hướng dương trắng 19',5,N'Mô tả SP 19',N'Trắng',N'Bó', 69000,29,0,5,'hinhanh19.jpg',1),
(N'Hoa hướng dương vàng 20', 5,N'Mô tả SP 20',N'Vàng', N'Hộp',70000,30,0,5,'hinhanh20.jpg',1),
(N'Hoa hồng đỏ 21',     1,N'Mô tả SP 21',N'Đỏ',   N'Giỏ',71000,31,0,5,'hinhanh21.jpg',1),
(N'Hoa hồng hồng 22',   1,N'Mô tả SP 22',N'Hồng', N'Bó', 72000,32,0,5,'hinhanh22.jpg',1),
(N'Hoa hồng trắng 23',  1,N'Mô tả SP 23',N'Trắng',N'Hộp',73000,33,0,5,'hinhanh23.jpg',1),
(N'Hoa hồng vàng 24',   1,N'Mô tả SP 24',N'Vàng', N'Giỏ',74000,34,0,5,'hinhanh24.jpg',1),
(N'Hoa lan đỏ 25',      2,N'Mô tả SP 25',N'Đỏ',   N'Bó', 75000,35,0,5,'hinhanh25.jpg',1),
(N'Hoa lan hồng 26',    2,N'Mô tả SP 26',N'Hồng', N'Hộp',76000,36,0,5,'hinhanh26.jpg',1),
(N'Hoa lan trắng 27',   2,N'Mô tả SP 27',N'Trắng',N'Giỏ',77000,37,0,5,'hinhanh27.jpg',1),
(N'Hoa lan vàng 28',    2,N'Mô tả SP 28',N'Vàng', N'Bó', 78000,38,0,5,'hinhanh28.jpg',1),
(N'Hoa tulip đỏ 29',    3,N'Mô tả SP 29',N'Đỏ',   N'Hộp',79000,39,0,5,'hinhanh29.jpg',1),
(N'Hoa tulip hồng 30',  3,N'Mô tả SP 30',N'Hồng', N'Giỏ',80000,40,0,5,'hinhanh30.jpg',1),
(N'Hoa tulip trắng 31', 3,N'Mô tả SP 31',N'Trắng',N'Bó', 81000,41,0,5,'hinhanh31.jpg',1),
(N'Hoa tulip vàng 32',  3,N'Mô tả SP 32',N'Vàng', N'Hộp',82000,42,0,5,'hinhanh32.jpg',1),
(N'Hoa cẩm tú cầu đỏ 33',   4,N'Mô tả SP 33',N'Đỏ',   N'Giỏ',83000,43,0,5,'hinhanh33.jpg',1),
(N'Hoa cẩm tú cầu hồng 34', 4,N'Mô tả SP 34',N'Hồng', N'Bó', 84000,44,0,5,'hinhanh34.jpg',1),
(N'Hoa cẩm tú cầu trắng 35',4,N'Mô tả SP 35',N'Trắng',N'Hộp',85000,45,0,5,'hinhanh35.jpg',1),
(N'Hoa cẩm tú cầu vàng 36', 4,N'Mô tả SP 36',N'Vàng', N'Giỏ',86000,46,0,5,'hinhanh36.jpg',1),
(N'Hoa hướng dương đỏ 37',  5,N'Mô tả SP 37',N'Đỏ',   N'Bó', 87000,47,0,5,'hinhanh37.jpg',1),
(N'Hoa hướng dương hồng 38',5,N'Mô tả SP 38',N'Hồng', N'Hộp',88000,48,0,5,'hinhanh38.jpg',1),
(N'Hoa hướng dương trắng 39',5,N'Mô tả SP 39',N'Trắng',N'Giỏ',89000,49,0,5,'hinhanh39.jpg',1),
(N'Hoa hướng dương vàng 40', 5,N'Mô tả SP 40',N'Vàng', N'Bó', 90000,10,0,5,'hinhanh40.jpg',1),
(N'Hoa hồng đỏ 41',     1,N'Mô tả SP 41',N'Đỏ',   N'Hộp',91000,11,0,5,'hinhanh41.jpg',1),
(N'Hoa hồng hồng 42',   1,N'Mô tả SP 42',N'Hồng', N'Giỏ',92000,12,0,5,'hinhanh42.jpg',1),
(N'Hoa hồng trắng 43',  1,N'Mô tả SP 43',N'Trắng',N'Bó', 93000,13,0,5,'hinhanh43.jpg',1),
(N'Hoa hồng vàng 44',   1,N'Mô tả SP 44',N'Vàng', N'Hộp',94000,14,0,5,'hinhanh44.jpg',1),
(N'Hoa lan đỏ 45',      2,N'Mô tả SP 45',N'Đỏ',   N'Giỏ',95000,15,0,5,'hinhanh45.jpg',1);
GO

-- PRODUCT_COMPONENT
INSERT INTO PRODUCT_COMPONENT (ProductID, FlowerID, Quantity)
SELECT ProductID, ProductTypeID, 10 FROM PRODUCT;
GO
INSERT INTO PRODUCT_COMPONENT (ProductID, FlowerID, Quantity)
SELECT ProductID, 6, 2 FROM PRODUCT;
GO
INSERT INTO PRODUCT_COMPONENT (ProductID, FlowerID, Quantity)
SELECT ProductID, 7, 5 FROM PRODUCT;
GO

-- GOODS_RECEIPT
INSERT INTO GOODS_RECEIPT (EmployeeID, ImportDate, Note) VALUES
(2,'2024-01-01',N'Phiếu nhập 1'),(2,'2024-01-02',N'Phiếu nhập 2'),
(2,'2024-01-03',N'Phiếu nhập 3'),(2,'2024-01-04',N'Phiếu nhập 4'),
(2,'2024-01-05',N'Phiếu nhập 5'),(2,'2024-01-06',N'Phiếu nhập 6'),
(2,'2024-01-07',N'Phiếu nhập 7'),(2,'2024-01-08',N'Phiếu nhập 8'),
(2,'2024-01-09',N'Phiếu nhập 9'),(2,'2024-01-10',N'Phiếu nhập 10'),
(2,'2024-01-11',N'Phiếu nhập 11'),(2,'2024-01-12',N'Phiếu nhập 12'),
(2,'2024-01-13',N'Phiếu nhập 13'),(2,'2024-01-14',N'Phiếu nhập 14'),
(2,'2024-01-15',N'Phiếu nhập 15'),(2,'2024-01-16',N'Phiếu nhập 16'),
(2,'2024-01-17',N'Phiếu nhập 17'),(2,'2024-01-18',N'Phiếu nhập 18'),
(2,'2024-01-19',N'Phiếu nhập 19'),(2,'2024-01-20',N'Phiếu nhập 20'),
(2,'2024-01-21',N'Phiếu nhập 21'),(2,'2024-01-22',N'Phiếu nhập 22'),
(2,'2024-01-23',N'Phiếu nhập 23'),(2,'2024-01-24',N'Phiếu nhập 24'),
(2,'2024-01-25',N'Phiếu nhập 25');
GO

-- GOODS_RECEIPT_DETAIL
INSERT INTO GOODS_RECEIPT_DETAIL (ReceiptID, ProductID, Quantity, ImportPrice) VALUES
(1,1,2,51000),(2,2,3,52000),(3,3,4,53000),(4,4,5,54000),
(5,5,6,55000),(6,6,7,56000),(7,7,8,57000),(8,8,9,58000),
(9,9,10,59000),(10,10,1,60000),(11,11,2,61000),(12,12,3,62000),
(13,13,4,63000),(14,14,5,64000),(15,15,6,65000),(16,16,7,66000),
(17,17,8,67000),(18,18,9,68000),(19,19,10,69000),(20,20,1,70000),
(21,21,2,71000),(22,22,3,72000),(23,23,4,73000),(24,24,5,74000),
(25,25,6,75000),
(1,26,7,76000),(2,27,8,77000),(3,28,9,78000),(4,29,10,79000),
(5,30,1,80000),(6,31,2,81000),(7,32,3,82000),(8,33,4,83000),
(9,34,5,84000),(10,35,6,85000),
(11,36,7,86000),(12,37,8,87000),(13,38,9,88000),(14,39,10,89000),
(15,40,3,90000),(16,41,3,91000),(17,42,3,92000),(18,43,3,93000),
(19,44,3,94000),(20,45,3,95000);
GO

-- CART
INSERT INTO CART (CustomerID, CreatedDate, UpdatedDate)
SELECT CustomerID, RegistrationDate, RegistrationDate FROM CUSTOMER;
GO

-- CART_DETAIL
INSERT INTO CART_DETAIL (CartID, ProductID, Quantity, AddedDate) VALUES
(1,1,2,'2024-01-06'),(2,2,3,'2024-01-07'),(3,3,4,'2024-01-08'),
(4,4,5,'2024-01-09'),(5,5,1,'2024-01-10'),(6,6,2,'2024-01-11'),
(7,7,3,'2024-01-12'),(8,8,4,'2024-01-13'),(9,9,5,'2024-01-14'),
(10,10,1,'2024-01-15'),(11,11,2,'2024-01-16'),(12,12,3,'2024-01-17'),
(13,13,4,'2024-01-18'),(14,14,5,'2024-01-19'),(15,15,1,'2024-01-20'),
(16,16,2,'2024-01-21'),(17,17,3,'2024-01-22'),(18,18,4,'2024-01-23'),
(19,19,5,'2024-01-24'),(20,20,1,'2024-01-25'),(21,21,2,'2024-01-26'),
(22,22,3,'2024-01-27'),(23,23,4,'2024-01-28');
GO

-- SALES_ORDER
INSERT INTO SALES_ORDER (CustomerID, EmployeeID, CreatedDate, OrderStatus) VALUES
(1, 1,'2024-01-06',N'Mới'),
(2, 1,'2024-01-07',N'Đang xử lý'),
(3, 1,'2024-01-08',N'Đang xử lý'),
(4, 1,'2024-01-09',N'Mới'),
(5, 2,'2024-01-10',N'Đang xử lý'),
(6, 2,'2024-01-11',N'Đang xử lý'),
(7, 1,'2024-01-12',N'Mới'),
(8, 1,'2024-01-13',N'Đang xử lý'),
(9, 1,'2024-01-14',N'Đang xử lý'),
(10,2,'2024-01-15',N'Mới'),
(11,1,'2024-01-16',N'Đang xử lý'),
(12,1,'2024-01-17',N'Đang xử lý'),
(13,2,'2024-01-18',N'Mới'),
(14,1,'2024-01-19',N'Đang xử lý'),
(15,1,'2024-01-20',N'Đang xử lý'),
(16,2,'2024-01-21',N'Mới'),
(17,1,'2024-01-22',N'Đang xử lý'),
(18,1,'2024-01-23',N'Đang xử lý'),
(19,2,'2024-01-24',N'Mới'),
(20,1,'2024-01-25',N'Đang xử lý'),
(21,1,'2024-01-26',N'Đang xử lý'),
(22,2,'2024-01-27',N'Mới'),
(23,1,'2024-01-28',N'Đang xử lý');
GO

-- ORDER_DETAIL
INSERT INTO ORDER_DETAIL (OrderID, ProductID, Quantity, UnitPrice, CostPrice, Discount) VALUES
(1, 1, 2, 56610, 51000, 0),
(2, 2, 3, 58240, 52000, 0),
(3, 3, 4, 59890, 53000, 0),
(4, 4, 5, 61560, 54000, 0),
(5, 5, 1, 63250, 55000, 0),
(6, 6, 2, 64960, 56000, 0),
(7, 7, 3, 66690, 57000, 0),
(8, 8, 4, 68440, 58000, 0),
(9, 9, 5, 70210, 59000, 0),
(10,10, 1, 72000, 60000, 0),
(11,11, 2, 73810, 61000, 0),
(12,12, 3, 75640, 62000, 0),
(13,13, 4, 77490, 63000, 0),
(14,14, 5, 79360, 64000, 0),
(15,15, 1, 81250, 65000, 0),
(16,16, 2, 83160, 66000, 0),
(17,17, 3, 85090, 67000, 0),
(18,18, 4, 87040, 68000, 0),
(19,19, 5, 89010, 69000, 0),
(20,20, 1, 91000, 70000, 0),
(21,21, 2, 93010, 71000, 0),
(22,22, 3, 95040, 72000, 0),
(23,23, 4, 97090, 73000, 0);
GO

-- PAYMENT (đổi N'Trực tiếp' → N'Tiền mặt')
INSERT INTO PAYMENT (OrderID, Method, Amount, Status, PaymentDate) VALUES
(1, N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=1), N'Chưa thanh toán', NULL),
(2, N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=2), N'Đã thanh toán','2024-01-07'),
(3, N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=3), N'Chưa thanh toán', NULL),
(4, N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=4), N'Đã thanh toán','2024-01-09'),
(5, N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=5), N'Chưa thanh toán', NULL),
(6, N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=6), N'Đã thanh toán','2024-01-11'),
(7, N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=7), N'Chưa thanh toán', NULL),
(8, N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=8), N'Đã thanh toán','2024-01-13'),
(9, N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=9), N'Chưa thanh toán', NULL),
(10,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=10),N'Đã thanh toán','2024-01-15'),
(11,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=11),N'Chưa thanh toán', NULL),
(12,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=12),N'Đã thanh toán','2024-01-17'),
(13,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=13),N'Chưa thanh toán', NULL),
(14,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=14),N'Đã thanh toán','2024-01-19'),
(15,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=15),N'Chưa thanh toán', NULL),
(16,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=16),N'Đã thanh toán','2024-01-21'),
(17,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=17),N'Chưa thanh toán', NULL),
(18,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=18),N'Đã thanh toán','2024-01-23'),
(19,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=19),N'Chưa thanh toán', NULL),
(20,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=20),N'Đã thanh toán','2024-01-25'),
(21,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=21),N'Chưa thanh toán', NULL),
(22,N'Chuyển khoản',(SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=22),N'Đã thanh toán','2024-01-27'),
(23,N'Tiền mặt',    (SELECT TotalPayment FROM vw_OrderSummary WHERE OrderID=23),N'Chưa thanh toán', NULL);
GO

-- DELIVERY
INSERT INTO DELIVERY (OrderID, ReceiverName, ReceiverPhone, DeliveryAddress, DeliveryStatus) VALUES
(1, N'Người nhận 1','0910000001',N'Địa chỉ giao 1', N'Chờ giao'),
(2, N'Người nhận 2','0910000002',N'Địa chỉ giao 2', N'Đã giao'),
(3, N'Người nhận 3','0910000003',N'Địa chỉ giao 3', N'Đang giao'),
(4, N'Người nhận 4','0910000004',N'Địa chỉ giao 4', N'Chờ giao'),
(5, N'Người nhận 5','0910000005',N'Địa chỉ giao 5', N'Đã giao'),
(6, N'Người nhận 6','0910000006',N'Địa chỉ giao 6', N'Đang giao'),
(7, N'Người nhận 7','0910000007',N'Địa chỉ giao 7', N'Chờ giao'),
(8, N'Người nhận 8','0910000008',N'Địa chỉ giao 8', N'Đã giao'),
(9, N'Người nhận 9','0910000009',N'Địa chỉ giao 9', N'Đang giao'),
(10,N'Người nhận 10','0910000010',N'Địa chỉ giao 10',N'Chờ giao'),
(11,N'Người nhận 11','0910000011',N'Địa chỉ giao 11',N'Đã giao'),
(12,N'Người nhận 12','0910000012',N'Địa chỉ giao 12',N'Đang giao'),
(13,N'Người nhận 13','0910000013',N'Địa chỉ giao 13',N'Chờ giao'),
(14,N'Người nhận 14','0910000014',N'Địa chỉ giao 14',N'Đã giao'),
(15,N'Người nhận 15','0910000015',N'Địa chỉ giao 15',N'Đang giao'),
(16,N'Người nhận 16','0910000016',N'Địa chỉ giao 16',N'Chờ giao'),
(17,N'Người nhận 17','0910000017',N'Địa chỉ giao 17',N'Đã giao'),
(18,N'Người nhận 18','0910000018',N'Địa chỉ giao 18',N'Đang giao'),
(19,N'Người nhận 19','0910000019',N'Địa chỉ giao 19',N'Chờ giao'),
(20,N'Người nhận 20','0910000020',N'Địa chỉ giao 20',N'Đã giao'),
(21,N'Người nhận 21','0910000021',N'Địa chỉ giao 21',N'Đang giao'),
(22,N'Người nhận 22','0910000022',N'Địa chỉ giao 22',N'Chờ giao'),
(23,N'Người nhận 23','0910000023',N'Địa chỉ giao 23',N'Đã giao');
GO


-- Đếm số dòng các bảng chính
SELECT N'ACCOUNT'        AS TableName, COUNT(*) AS SoDong FROM ACCOUNT
UNION ALL SELECT N'CUSTOMER',      COUNT(*) FROM CUSTOMER
UNION ALL SELECT N'EMPLOYEE',      COUNT(*) FROM EMPLOYEE
UNION ALL SELECT N'PRODUCT',       COUNT(*) FROM PRODUCT
UNION ALL SELECT N'PRODUCT_TYPE',  COUNT(*) FROM PRODUCT_TYPE
UNION ALL SELECT N'FLOWER',        COUNT(*) FROM FLOWER
UNION ALL SELECT N'PRODUCT_COMPONENT', COUNT(*) FROM PRODUCT_COMPONENT
UNION ALL SELECT N'GOODS_RECEIPT', COUNT(*) FROM GOODS_RECEIPT
UNION ALL SELECT N'SALES_ORDER',   COUNT(*) FROM SALES_ORDER
UNION ALL SELECT N'ORDER_DETAIL',  COUNT(*) FROM ORDER_DETAIL
UNION ALL SELECT N'PAYMENT',       COUNT(*) FROM PAYMENT
UNION ALL SELECT N'DELIVERY',      COUNT(*) FROM DELIVERY;
GO