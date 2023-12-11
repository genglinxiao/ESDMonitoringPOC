CREATE TABLE Suppliers (
    SupplierID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Country VARCHAR(100),
    Website VARCHAR(255)
);

CREATE TABLE Manufacturers (
    ManufacturerID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Country VARCHAR(100),
    Website VARCHAR(255)
);

CREATE TABLE Devices (
    DeviceID INT AUTO_INCREMENT PRIMARY KEY,
    Model VARCHAR(255) NOT NULL,
    ManufacturerID INT,
    Description TEXT,
    ManufactureDate DATE,
    WarrantyPeriod INT,  -- Warranty period in months or years, depending on your preference
    AdditionalInfo TEXT, -- Text field for future use
    FOREIGN KEY (ManufacturerID) REFERENCES Manufacturers(ManufacturerID)
);

CREATE TABLE Modules (
    ModuleID INT AUTO_INCREMENT PRIMARY KEY,
    Type VARCHAR(100) NOT NULL,
    Description TEXT
);

CREATE TABLE Parts (
    PartID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Type VARCHAR(100) NOT NULL,
    SupplierID INT,
    Specification TEXT,
    FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);

CREATE TABLE DeviceModules (
    DeviceID INT,
    ModuleID INT,
    Quantity INT,
    PRIMARY KEY (DeviceID, ModuleID),
    FOREIGN KEY (DeviceID) REFERENCES Devices(DeviceID),
    FOREIGN KEY (ModuleID) REFERENCES Modules(ModuleID)
);

CREATE TABLE ModuleParts (
    ModuleID INT,
    PartID INT,
    Quantity INT,
    PRIMARY KEY (ModuleID, PartID),
    FOREIGN KEY (ModuleID) REFERENCES Modules(ModuleID),
    FOREIGN KEY (PartID) REFERENCES Parts(PartID)
);
