USE BonusTest;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Bonus (
    BonusId INT IDENTITY(1,1) CONSTRAINT PK_Bonus PRIMARY KEY,
    DepartmentCode CHAR(5) NOT NULL,
    EmployeeNumber INT NOT NULL,
    Period DATE NOT NULL,
    BonusTypeId INT NOT NULL,
    Value DECIMAL(20,4) NOT NULL,
    ReasonCode CHAR(6) NOT NULL,
    SalarySnapshot DECIMAL(16,2) NOT NULL,
    TaxRateSnapshot DECIMAL(5,2) NOT NULL,
    GrossAmount DECIMAL(16,2) NOT NULL,
    TaxAmount DECIMAL(16,2) NOT NULL,
    NetAmount DECIMAL(16,2) NOT NULL,
    StateCode CHAR(4) NOT NULL CONSTRAINT DF_Bonus_State DEFAULT '0201',
    IsDeleted BIT NOT NULL CONSTRAINT DF_Bonus_IsDeleted DEFAULT 0,
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL,
    ModifiedBy NVARCHAR(100) NOT NULL,
    ModifiedAt DATETIME2(7) NOT NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT FK_Bonus_Employee FOREIGN KEY (DepartmentCode, EmployeeNumber)
    REFERENCES dbo.Employee(DepartmentCode, EmployeeNumber),
    CONSTRAINT FK_Bonus_Type FOREIGN KEY (BonusTypeId) REFERENCES dbo.BonusType(BonusTypeId),
    CONSTRAINT FK_Bonus_Reason FOREIGN KEY (ReasonCode) REFERENCES dbo.BonusReason(ReasonCode),
    CONSTRAINT FK_Bonus_State FOREIGN KEY (StateCode) REFERENCES dbo.State(StateCode),
    CONSTRAINT CK_Bonus_State CHECK (StateCode IN ('0201','0202','0203')),
    CONSTRAINT CK_Bonus_Period CHECK (DAY(Period) = 1),
    CONSTRAINT CK_Bonus_FinancialData CHECK (SalarySnapshot > 0 AND TaxRateSnapshot BETWEEN 0 AND 100),
    CONSTRAINT CK_Bonus_Value CHECK (Value > 0 AND ((BonusTypeId = 1 AND Value <= SalarySnapshot) OR (BonusTypeId = 2 AND Value <= 100))),
    CONSTRAINT CK_Bonus_Amounts CHECK (GrossAmount >= 0 AND TaxAmount >= 0 AND NetAmount >= 0 AND NetAmount = GrossAmount - TaxAmount),
    CONSTRAINT CK_Bonus_Audit CHECK (LEN(LTRIM(RTRIM(CreatedBy))) > 0 AND LEN(LTRIM(RTRIM(ModifiedBy))) > 0 AND ModifiedAt >= CreatedAt)
);

-- Garantiza la unicidad incluso cuando solicitudes concurrentes superan, ambas, la validación de la aplicación.
CREATE UNIQUE INDEX UX_Bonus_ActiveEmployeePeriod
    ON dbo.Bonus(DepartmentCode, EmployeeNumber, Period)
    WHERE IsDeleted = 0 AND StateCode <> '0203';

-- Admite paginación ordenada predeterminada y los filtros habituales por período o estado, excluyendo las filas eliminadas.
CREATE INDEX IX_Bonus_Period_State
    ON dbo.Bonus(Period DESC, StateCode, BonusId DESC)
    INCLUDE (DepartmentCode, EmployeeNumber)
    WHERE IsDeleted = 0;

COMMIT;
GO
