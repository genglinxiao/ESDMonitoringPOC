INSERT INTO Suppliers (Name, Country, Website) VALUES 
('ElectroMax Innovations', 'Germany', 'http://electromax-innovations.com'),
('Quantum Energy Solutions', 'USA', 'http://quantumenergysolutions.com'),
('NanoTech Power Corp', 'Japan', 'http://nanotechpowercorp.com'),
('GreenVolt Dynamics', 'Sweden', 'http://greenvoltdynamics.com'),
('FutureBatt Industries', 'South Korea', 'http://futurebattindustries.com');

INSERT INTO Manufacturers (Name, Country, Website) VALUES 
('GigaWatt Devices', 'USA', 'http://gigawattdevices.com'),
('EcoEnergy Systems', 'China', 'http://ecoenergysystems.com'),
('PowerPlex Technologies', 'India', 'http://powerplextechnologies.com'),
('VoltVanguard Ltd', 'UK', 'http://voltvanguard.co.uk'),
('TerraWatt Innovations', 'Canada', 'http://terrawattinnovations.com');

INSERT INTO Devices (Model, ManufacturerID, Description, ManufactureDate, WarrantyPeriod) VALUES 
('AlphaX100', 1, 'High-capacity industrial storage', '2022-01-10', 36),
('BetaEco200', 2, 'Eco-friendly residential battery', '2022-03-15', 24),
('GammaPower50', 3, 'Compact portable power unit', '2022-05-22', 18),
('DeltaVolt500', 4, 'Advanced commercial energy system', '2022-07-30', 48),
('EpsilonMini45', 5, 'Small-scale renewable storage', '2022-09-11', 12);

INSERT INTO Modules (Type, Description) VALUES 
('Input Module', 'High-efficiency input module'),
('Output Module', 'Stable output module for various loads'),
('Control Module', 'Smart control and monitoring module'),
('Safety Module', 'Safety and protection module'),
('Communication Module', 'Wireless communication and data module');

INSERT INTO Parts (Name, Type, SupplierID, Specification) VALUES 
('LithiumCell-3000', 'Cell', 1, '3000mAh Li-ion'),
('NiMHBattery-2500', 'Cell', 2, '2500mAh NiMH'),
('SmartController-ZX', 'Controller', 3, 'ZX Series Smart Controller'),
('PowerRegulator-Y', 'Regulator', 4, 'Y Series Voltage Regulator'),
('ThermalSensor-T100', 'Sensor', 5, 'T100 Thermal Sensor'),
('WirelessModule-W5G', 'Communication', 3, 'W5G Wireless Communication Module');

INSERT INTO DeviceModules (DeviceID, ModuleID, Quantity) VALUES 
(1, 1, 2),
(1, 3, 1),
(2, 1, 1),
(2, 2, 1),
(2, 4, 1),
(3, 1, 1),
(3, 3, 1),
(3, 5, 1),
(4, 2, 2),
(4, 3, 1),
(4, 4, 1),
(5, 1, 1),
(5, 2, 1),
(5, 5, 1);

INSERT INTO ModuleParts (ModuleID, PartID, Quantity) VALUES 
(1, 1, 6),
(1, 3, 1),
(2, 2, 4),
(2, 4, 1),
(3, 3, 2),
(3, 5, 2),
(4, 4, 1),
(4, 6, 1),
(5, 6, 1);
