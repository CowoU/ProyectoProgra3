-- ============================================================================
-- SCRIPT DE INICIALIZACIÓN - Portal de Pagos
-- Base de Datos: db_pago_servicios
-- Sistema Gestor: MySQL 8.0+
-- ============================================================================

-- Crear la base de datos si no existe
CREATE DATABASE IF NOT EXISTS db_pago_servicios;
USE db_pago_servicios;

-- ============================================================================
-- 1. TABLA DE USUARIOS
-- ============================================================================
DROP TABLE IF EXISTS `Cuotas`;
DROP TABLE IF EXISTS `Transacciones`;
DROP TABLE IF EXISTS `Usuarios`;
DROP TABLE IF EXISTS `Empresas`;

CREATE TABLE `Usuarios` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `nombre` VARCHAR(100) NOT NULL,
    `pin` VARCHAR(10) NOT NULL,
    `rol` VARCHAR(20) NOT NULL COMMENT 'Cliente, Cajero, Admin',
    `saldo_bancario` DECIMAL(18,2) DEFAULT 0.00
) COMMENT='Tabla de Usuarios del Sistema';

-- ============================================================================
-- 2. TABLA DE EMPRESAS
-- ============================================================================
CREATE TABLE `Empresas` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `nombre` VARCHAR(100) NOT NULL,
    `saldo_acumulado` DECIMAL(18,2) DEFAULT 0.00
) COMMENT='Tabla de Empresas Prestadoras de Servicios';

-- ============================================================================
-- 3. TABLA DE CUOTAS (Mensualidades)
-- ============================================================================
CREATE TABLE `Cuotas` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `usuario_id` INT NOT NULL,
    `empresa_id` INT NOT NULL,
    `mes` VARCHAR(20) NOT NULL,
    `monto` DECIMAL(18,2) NOT NULL,
    `estado` VARCHAR(20) DEFAULT 'Pendiente' COMMENT 'Pendiente, Pagado',
    FOREIGN KEY (`usuario_id`) REFERENCES `Usuarios`(`id`),
    FOREIGN KEY (`empresa_id`) REFERENCES `Empresas`(`id`)
) COMMENT='Tabla de Cuotas a Pagar';

-- ============================================================================
-- 4. BITÁCORA DE TRANSACCIONES (Para el reporte del Banco)
-- ============================================================================
CREATE TABLE `Transacciones` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `usuario_id` INT,
    `empresa_id` INT,
    `monto_total` DECIMAL(18,2),
    `comision_banco` DECIMAL(18,2),
    `pago_empresa` DECIMAL(18,2),
    `fecha` DATETIME DEFAULT CURRENT_TIMESTAMP
) COMMENT='Bitácora de Transacciones de Pagos';

-- ============================================================================
-- DATOS DE PRUEBA
-- ============================================================================

-- Insertar Empresas
INSERT INTO `Empresas` (`nombre`, `saldo_acumulado`) VALUES 
('Cementerio El Descanso', 0.00),
('Condominio Las Flores', 0.00);

-- Insertar Usuarios (Clientes, Cajero y Admin)
INSERT INTO `Usuarios` (`nombre`, `pin`, `rol`, `saldo_bancario`) VALUES 
('Juan Carlos García', '1234', 'Cliente', 5000.00),
('María Elena López', '5678', 'Cliente', 3500.00),
('Carlos Mendoza', '9012', 'Cliente', 8000.00),
('Laura Martínez', '2468', 'Cajero', 0.00),
('Roberto García', '1357', 'Admin', 0.00);

-- Insertar Cuotas pendientes de prueba
INSERT INTO `Cuotas` (`usuario_id`, `empresa_id`, `mes`, `monto`, `estado`) VALUES 
(1, 1, 'Enero', 150.00, 'Pendiente'),
(1, 2, 'Enero', 300.00, 'Pendiente'),
(2, 1, 'Enero', 150.00, 'Pendiente'),
(3, 2, 'Febrero', 300.00, 'Pendiente');

-- ============================================================================
-- VERIFICACIÓN
-- ============================================================================
-- Listar datos cargados
SELECT 'USUARIOS:' AS Descripcion;
SELECT * FROM `Usuarios`;

SELECT 'EMPRESAS:' AS Descripcion;
SELECT * FROM `Empresas`;

SELECT 'CUOTAS:' AS Descripcion;
SELECT * FROM `Cuotas`;
