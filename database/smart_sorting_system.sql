-- 1. 데이터베이스 생성
CREATE DATABASE IF NOT EXISTS smart_sorting_system
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

-- 2. 데이터베이스 선택
USE smart_sorting_system;

-- 3. 사용자 테이블 생성
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    login_id VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    name VARCHAR(50) NOT NULL,
    role VARCHAR(20) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_users_role
        CHECK (role IN ('ADMIN', 'WORKER'))
);

-- 4. 제품 유형 테이블 생성
CREATE TABLE product_types (
    product_type_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE,
    product_name VARCHAR(50) NOT NULL,
    unit_per_set INT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_product_types_unit_per_set
        CHECK (unit_per_set > 0)
);

-- 5. 제품 유형 초기 데이터
INSERT INTO product_types (
    code,
    product_name,
    unit_per_set
) VALUES
    ('CHOCOLATE', '초콜릿', 10),
    ('CANDY', '사탕', 1);

-- 6. 시스템 구성요소 테이블 생성
CREATE TABLE system_components (
    component_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    component_name VARCHAR(50) NOT NULL,
    component_type VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
    status_updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_system_components_type
        CHECK (component_type IN (
            'SENSOR',
            'ACTUATOR',
            'DISPLAY',
            'SOFTWARE',
            'SERVER',
            'DATABASE'
        )),

    CONSTRAINT chk_system_components_status
        CHECK (status IN (
            'NORMAL',
            'WARNING',
            'ERROR',
            'OFFLINE'
        ))
);

