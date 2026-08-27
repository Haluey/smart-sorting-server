-- =========================================================
-- Smart Sorting System Database
-- 최종 정리본
--
-- 주의:
-- 이 스크립트를 실행하면 기존 smart_sorting_system DB를 삭제하고
-- 처음부터 다시 생성합니다.
--
-- DBeaver 실행 방법:
-- 현재 SQL문 실행    : Ctrl + Enter
-- SQL 스크립트 전체 실행 : Alt + X
-- =========================================================

DROP DATABASE IF EXISTS smart_sorting_system;

CREATE DATABASE smart_sorting_system
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE smart_sorting_system;


-- =========================================================
-- 1. 사용자 테이블
-- =========================================================

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


-- =========================================================
-- 2. 제품 유형 테이블
-- =========================================================

CREATE TABLE product_types (
    product_type_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    product_type_code VARCHAR(20) NOT NULL UNIQUE,
    product_name VARCHAR(50) NOT NULL,
    unit_per_set INT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_product_types_unit_per_set
        CHECK (unit_per_set > 0)
);


-- =========================================================
-- 3. 시스템 구성요소 테이블
-- =========================================================

CREATE TABLE system_components (
    component_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    component_code VARCHAR(30) NOT NULL UNIQUE,
    component_name VARCHAR(50) NOT NULL,
    component_type VARCHAR(20) NOT NULL,
    current_status VARCHAR(20) NOT NULL,
    status_updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_system_components_type
        CHECK (component_type IN (
            'SENSOR',
            'ACTUATOR',
            'CONTROLLER',
            'DISPLAY',
            'SOFTWARE',
            'SERVER',
            'DATABASE'
        )),

    CONSTRAINT chk_system_components_status
        CHECK (current_status IN (
            'NORMAL',
            'WARNING',
            'ERROR',
            'OFFLINE'
        ))
);


-- =========================================================
-- 4. 생산 목표 테이블
-- =========================================================

CREATE TABLE production_targets (
    target_id INT PRIMARY KEY,
    target_chocolate_set_count INT NOT NULL,
    target_candy_set_count INT NOT NULL,
    updated_at DATETIME NOT NULL
        DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT chk_production_targets_chocolate
        CHECK (target_chocolate_set_count > 0),

    CONSTRAINT chk_production_targets_candy
        CHECK (target_candy_set_count > 0)
);


-- =========================================================
-- 5. 생산 세션 테이블
-- =========================================================

CREATE TABLE production_sessions (
    session_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,

    target_chocolate_set_count INT NOT NULL,
    target_candy_set_count INT NOT NULL,

    chocolate_count INT NOT NULL DEFAULT 0,
    candy_count INT NOT NULL DEFAULT 0,

    status VARCHAR(20) NOT NULL DEFAULT 'RUNNING',

    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at DATETIME NULL,
    updated_at DATETIME NOT NULL
        DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_production_sessions_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id),

    CONSTRAINT chk_production_sessions_target_chocolate
        CHECK (target_chocolate_set_count >= 0),

    CONSTRAINT chk_production_sessions_target_candy
        CHECK (target_candy_set_count >= 0),

    CONSTRAINT chk_production_sessions_target
        CHECK (
            target_chocolate_set_count > 0
            OR target_candy_set_count > 0
        ),

    CONSTRAINT chk_production_sessions_chocolate_count
        CHECK (chocolate_count >= 0),

    CONSTRAINT chk_production_sessions_candy_count
        CHECK (candy_count >= 0),

    CONSTRAINT chk_production_sessions_status
        CHECK (status IN (
            'RUNNING',
            'PAUSED',
            'COMPLETED',
            'CANCELLED'
        )),

    CONSTRAINT chk_production_sessions_ended_at
        CHECK (
            (
                status IN ('RUNNING', 'PAUSED')
                AND ended_at IS NULL
            )
            OR
            (
                status IN ('COMPLETED', 'CANCELLED')
                AND ended_at IS NOT NULL
            )
        )
);


-- =========================================================
-- 6. 제품 감지 및 분류 결과 테이블
-- =========================================================

CREATE TABLE product_detections (
    product_detection_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    session_id BIGINT NOT NULL,
    product_type_id BIGINT NULL,

    confidence DECIMAL(5, 4) NULL,
    image_path VARCHAR(255) NULL,
    classification_status VARCHAR(20) NOT NULL,

    detected_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_product_detections_session
        FOREIGN KEY (session_id)
        REFERENCES production_sessions(session_id),

    CONSTRAINT fk_product_detections_product_type
        FOREIGN KEY (product_type_id)
        REFERENCES product_types(product_type_id),

    CONSTRAINT chk_product_detections_confidence
        CHECK (
            confidence IS NULL
            OR (confidence >= 0 AND confidence <= 1)
        ),

    CONSTRAINT chk_product_detections_classification_status
        CHECK (classification_status IN (
            'SUCCESS',
            'FAILED'
        )),

    CONSTRAINT chk_product_detections_result
        CHECK (
            (
                classification_status = 'SUCCESS'
                AND product_type_id IS NOT NULL
                AND confidence IS NOT NULL
            )
            OR
            (
                classification_status = 'FAILED'
                AND product_type_id IS NULL
            )
        )
);


-- =========================================================
-- 7. 알림 테이블
-- =========================================================

CREATE TABLE alerts (
    alert_id BIGINT AUTO_INCREMENT PRIMARY KEY,

    session_id BIGINT NULL,
    component_id BIGINT NULL,
    product_detection_id BIGINT NULL,
    checked_by_user_id BIGINT NULL,

    alert_type VARCHAR(20) NOT NULL,
    priority VARCHAR(20) NOT NULL,

    recovery_status VARCHAR(20) NULL,
    check_status VARCHAR(20) NULL,

    alert_message VARCHAR(1000) NOT NULL,

    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    recovered_at DATETIME NULL,
    checked_at DATETIME NULL,

    CONSTRAINT fk_alerts_session
        FOREIGN KEY (session_id)
        REFERENCES production_sessions(session_id),

    CONSTRAINT fk_alerts_component
        FOREIGN KEY (component_id)
        REFERENCES system_components(component_id),

    CONSTRAINT fk_alerts_product_detection
        FOREIGN KEY (product_detection_id)
        REFERENCES product_detections(product_detection_id),

    CONSTRAINT fk_alerts_checked_user
        FOREIGN KEY (checked_by_user_id)
        REFERENCES users(user_id),

    CONSTRAINT chk_alerts_type
        CHECK (alert_type IN (
            'INFO',
            'WARNING',
            'ERROR'
        )),

    CONSTRAINT chk_alerts_priority
        CHECK (priority IN (
            'LOW',
            'MEDIUM',
            'HIGH'
        )),

    CONSTRAINT chk_alerts_recovery_status
        CHECK (
            recovery_status IS NULL
            OR recovery_status IN (
                'NOT_RECOVERED',
                'RECOVERED'
            )
        ),

    CONSTRAINT chk_alerts_check_status
        CHECK (
            check_status IS NULL
            OR check_status IN (
                'UNCHECKED',
                'CHECKED'
            )
        ),

    CONSTRAINT chk_alerts_type_priority
        CHECK (
            (alert_type = 'INFO' AND priority = 'LOW')
            OR
            (alert_type = 'WARNING' AND priority IN ('MEDIUM', 'HIGH'))
            OR
            (alert_type = 'ERROR' AND priority IN ('LOW', 'MEDIUM', 'HIGH'))
        ),

    CONSTRAINT chk_alerts_type_status
        CHECK (
            (
                alert_type = 'INFO'
                AND recovery_status IS NULL
                AND check_status IS NULL
            )
            OR
            (
                alert_type IN ('WARNING', 'ERROR')
                AND recovery_status IS NOT NULL
                AND check_status IS NOT NULL
            )
        ),

    CONSTRAINT chk_alerts_recovery_details
        CHECK (
            (
                recovery_status IS NULL
                AND recovered_at IS NULL
            )
            OR
            (
                recovery_status = 'NOT_RECOVERED'
                AND recovered_at IS NULL
            )
            OR
            (
                recovery_status = 'RECOVERED'
                AND recovered_at IS NOT NULL
            )
        ),

    CONSTRAINT chk_alerts_check_details
        CHECK (
            (
                check_status IS NULL
                AND checked_by_user_id IS NULL
                AND checked_at IS NULL
            )
            OR
            (
                check_status = 'UNCHECKED'
                AND checked_by_user_id IS NULL
                AND checked_at IS NULL
            )
            OR
            (
                check_status = 'CHECKED'
                AND checked_by_user_id IS NOT NULL
                AND checked_at IS NOT NULL
            )
        )
);


-- =========================================================
-- 초기 데이터
-- =========================================================

INSERT INTO product_types (
    product_type_code,
    product_name,
    unit_per_set
) VALUES
    ('CHOCOLATE', '초콜릿', 10),
    ('CANDY', '사탕', 1);


-- CAMERA는 Picamera2 + YOLO 제품 분류 기능을 함께 담당
-- 기존 VISION_MODULE은 사용하지 않음
-- WORKER_DISPLAY는 작업자용 LCD 장비로 유지
INSERT INTO system_components (
    component_code,
    component_name,
    component_type,
    current_status
) VALUES
    ('RASPBERRY_PI', '라즈베리파이 5', 'CONTROLLER', 'OFFLINE'),
    ('ARDUINO', '아두이노 제어 보드', 'CONTROLLER', 'OFFLINE'),

    ('IR_SENSOR', '제품 투입 감지 센서', 'SENSOR', 'OFFLINE'),
    ('CAMERA', '제품 분류 카메라 및 YOLO', 'SENSOR', 'OFFLINE'),

    ('CONVEYOR', '컨베이어 벨트', 'ACTUATOR', 'OFFLINE'),
    ('SORTING_SERVO', '제품 분류 서보모터', 'ACTUATOR', 'OFFLINE'),
    ('BUZZER', '알림 부저', 'ACTUATOR', 'OFFLINE'),

    ('WORKER_DISPLAY', '작업자 LCD 장비', 'DISPLAY', 'OFFLINE'),

    ('WORKER_UI', '작업자 화면 프로그램', 'SOFTWARE', 'OFFLINE'),
    ('ADMIN_WEB', '관리자 웹 프로그램', 'SOFTWARE', 'OFFLINE'),
    ('MQTT_BROKER', 'MQTT 브로커', 'SOFTWARE', 'OFFLINE'),

    ('API_SERVER', 'ASP.NET Core API 서버', 'SERVER', 'OFFLINE'),
    ('MYSQL_DATABASE', 'MySQL 데이터베이스', 'DATABASE', 'OFFLINE');


INSERT INTO production_targets (
    target_id,
    target_chocolate_set_count,
    target_candy_set_count
) VALUES (
    1,
    10,
    100
);


-- 기존 테스트 계정 BCrypt 해시값 직접 적용
INSERT INTO users (
    login_id,
    password_hash,
    name,
    role
) VALUES
    (
        'admin01',
        '$2a$11$w1I.A2g3AusRARbEFsk5LuZC/ArXFWvyyLsUQZyO4iOBWJIOerrja',
        '관리자',
        'ADMIN'
    ),
    (
        'worker01',
        '$2a$11$YZfLhNms4lQpjgQNk3z4O.UEsg8NYXsQrjw8II7KA2v.jwO99quni',
        '김작업',
        'WORKER'
    );


-- =========================================================
-- 테스트용 더미 데이터
-- =========================================================
-- 초기화 직후 RUNNING 세션이 남지 않도록
-- 두 번째 테스트 세션은 CANCELLED 상태로 저장
-- =========================================================

INSERT INTO production_sessions (
    user_id,
    target_chocolate_set_count,
    target_candy_set_count,
    chocolate_count,
    candy_count,
    status,
    started_at,
    ended_at
) VALUES
    (
        (
            SELECT user_id
            FROM users
            WHERE login_id = 'worker01'
        ),
        5,
        20,
        50,
        20,
        'COMPLETED',
        '2026-08-04 09:00:00',
        '2026-08-04 10:30:00'
    ),
    (
        (
            SELECT user_id
            FROM users
            WHERE login_id = 'worker01'
        ),
        10,
        30,
        24,
        12,
        'CANCELLED',
        '2026-08-05 13:00:00',
        '2026-08-05 14:00:00'
    );


INSERT INTO product_detections (
    session_id,
    product_type_id,
    confidence,
    image_path,
    classification_status,
    detected_at
) VALUES
    (
        (
            SELECT session_id
            FROM production_sessions
            ORDER BY session_id DESC
            LIMIT 1
        ),
        (
            SELECT product_type_id
            FROM product_types
            WHERE product_type_code = 'CHOCOLATE'
        ),
        0.9625,
        'uploads/detections/chocolate_001.jpg',
        'SUCCESS',
        '2026-08-05 13:05:10'
    ),
    (
        (
            SELECT session_id
            FROM production_sessions
            ORDER BY session_id DESC
            LIMIT 1
        ),
        NULL,
        0.0000,
        'uploads/detections/failed_001.jpg',
        'FAILED',
        '2026-08-05 13:06:20'
    );


INSERT INTO alerts (
    session_id,
    component_id,
    product_detection_id,
    checked_by_user_id,
    alert_type,
    priority,
    recovery_status,
    check_status,
    alert_message,
    created_at,
    recovered_at,
    checked_at
) VALUES
    (
        (
            SELECT session_id
            FROM production_sessions
            ORDER BY session_id DESC
            LIMIT 1
        ),
        NULL,
        NULL,
        NULL,
        'INFO',
        'LOW',
        NULL,
        NULL,
        '생산 작업이 정상적으로 시작되었습니다.',
        '2026-08-05 13:00:00',
        NULL,
        NULL
    ),

    -- 과거 테스트 오류이므로 현재 장비 상태와 충돌하지 않도록 RECOVERED 처리
    (
        (
            SELECT session_id
            FROM production_sessions
            ORDER BY session_id DESC
            LIMIT 1
        ),
        (
            SELECT component_id
            FROM system_components
            WHERE component_code = 'CAMERA'
        ),
        (
            SELECT product_detection_id
            FROM product_detections
            WHERE classification_status = 'FAILED'
            ORDER BY product_detection_id DESC
            LIMIT 1
        ),
        NULL,
        'ERROR',
        'HIGH',
        'RECOVERED',
        'UNCHECKED',
        '카메라 촬영 결과에서 제품 유형을 식별하지 못했습니다.',
        '2026-08-05 13:06:20',
        '2026-08-05 13:10:00',
        NULL
    );


-- =========================================================
-- 초기화 결과 확인
-- =========================================================

SELECT
    component_id,
    component_code,
    component_name,
    component_type,
    current_status
FROM system_components
ORDER BY component_id;

SELECT
    target_id,
    target_chocolate_set_count,
    target_candy_set_count,
    updated_at
FROM production_targets;

SELECT
    session_id,
    status,
    started_at,
    ended_at
FROM production_sessions
ORDER BY session_id;
