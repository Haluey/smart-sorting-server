# 개발 및 설계 진행 기록

스마트 분류 시스템 서버와 데이터베이스를 설계하면서 검토한 내용, 변경 이유, 완료 작업을 날짜별로 정리한 문서입니다.

---

## 2026-07-31 — 초기 데이터베이스 구조 정리

### 진행 내용

- 생산 작업을 중심으로 사용자, 제품, 제품 유형, 시스템 구성요소, 알림 구조를 정리했습니다.
- 초기 ERD에 다음 6개 테이블을 구성했습니다.
  - `USERS`
  - `PRODUCTION_SESSIONS`
  - `PRODUCTS`
  - `PRODUCT_TYPES`
  - `SYSTEM_COMPONENTS`
  - `ALERTS`
- 모든 테이블 관계를 비식별 관계로 설계했습니다.
- 작업자별 생산량과 제품별 분류 결과, 장비 오류 이력을 연결할 수 있도록 관계를 정의했습니다.

### 초기 설계에서 확인된 문제

- `PRODUCTS`라는 이름이 제품 기준 정보 테이블처럼 보일 수 있었습니다.
- `production_date`와 `started_at`은 날짜 정보가 중복될 수 있었습니다.
- `chocolate_set_count`는 `chocolate_count`에서 계산할 수 있었습니다.
- `PRODUCT_TYPES`의 `unit_type`, `set_quantity`, `is_active`는 현재 고정 제품 구조에 비해 복잡했습니다.
- `ALERTS`의 `result_status`, `status`, `severity`는 역할이 바로 드러나지 않았습니다.
- 알림에 발생 사용자와 확인 사용자를 함께 저장해 의미가 겹칠 수 있었습니다.
- 시스템 구성요소를 이름으로만 조회하면 프로그램 연동 시 이름 변경의 영향을 받을 수 있었습니다.

---

## 2026-08-03 — 데이터베이스 구조 초안 수정

### 진행 내용

- 기존 ERD를 기준으로 실제 구현에 필요한 테이블과 컬럼을 다시 검토했습니다.
- `USERS`, `PRODUCT_TYPES`, `SYSTEM_COMPONENTS` 테이블을 SQL로 옮기기 위한 초안을 작성했습니다.
- 제품 유형과 시스템 구성요소에 프로그램 연동용 고정 식별값을 추가하기로 했습니다.
- 일반적인 `code` 대신 역할이 드러나는 컬럼명으로 정리했습니다.
  - `product_type_code`
  - `component_code`
- 시스템 구성요소의 상태 컬럼을 `status`에서 `current_status`로 변경했습니다.
- Raspberry Pi와 Arduino를 표현하기 위해 구성요소 유형에 `CONTROLLER`를 추가했습니다.
- 시스템 구성요소 상태를 `NORMAL`, `WARNING`, `ERROR`, `OFFLINE`으로 정리했습니다.
- 시스템 실행 전 초기 상태는 `OFFLINE`으로 등록하기로 했습니다.

### 검토 및 결정 사항

- 시스템 구성요소 테이블에는 현재 상태만 저장하고, 상세 오류 내용과 과거 이력은 `alerts`에서 관리하기로 했습니다.
- 제품 투입 감지용 적외선 센서는 실제 구성에 맞춰 1개만 등록하기로 했습니다.
- 서보모터는 제품 분류 및 배출용 1개로 정리했습니다.
- 작업자 LCD 장비와 작업자 화면 프로그램은 하드웨어와 소프트웨어 상태를 구분하기 위해 각각 관리하기로 했습니다.

---

## 2026-08-04 — SQL 스키마 작성 및 컬럼 구조 개선

### 진행 내용

- MySQL 데이터베이스 생성 스크립트를 작성했습니다.
- 문자 집합을 `utf8mb4`, 정렬 규칙을 `utf8mb4_unicode_ci`로 설정했습니다.
- 모든 기본 키와 외래키 타입을 `BIGINT`로 통일했습니다.
- MySQL `ENUM` 대신 `VARCHAR`와 `CHECK` 제약조건을 사용하도록 변경했습니다.

### 사용자 테이블

- `password`를 `password_hash`로 변경했습니다.
- `login_id`, `name`의 길이를 `VARCHAR(50)`으로 확장했습니다.
- 역할을 `ADMIN`, `WORKER`로 제한했습니다.

### 제품 유형 테이블

- 서버에서 사용할 수 있는 `product_type_code`를 추가했습니다.
- `unit_type`, `set_quantity`를 `unit_per_set`으로 단순화했습니다.
- 현재 초콜릿과 사탕만 고정으로 사용하므로 `is_active`를 제거했습니다.
- 초콜릿 10개, 사탕 1개를 초기 데이터로 정의했습니다.

### 시스템 구성요소 테이블

- 서버와 MQTT에서 이름 대신 안정적으로 조회하도록 `component_code`를 추가했습니다.
- `status`를 `current_status`로 변경했습니다.
- `updated_at`을 `status_updated_at`으로 변경해 의미를 명확히 했습니다.
- 라즈베리파이, 아두이노, 센서, 카메라, 컨베이어, 서보모터, 부저, 화면, 프로그램, 서버, DB를 초기 데이터로 등록했습니다.

### 생산 작업 테이블

- 작업 시작 시 행이 생성되는 구조로 결정했습니다.
- `production_date`를 제거하고 `started_at`에서 날짜를 계산하도록 했습니다.
- `chocolate_set_count`를 제거하고 낱개 수와 제품 유형 기준으로 계산하도록 했습니다.
- 상태를 `RUNNING`, `PAUSED`, `COMPLETED`, `CANCELLED`로 정의했습니다.

---

## 2026-08-05 — 데이터 정합성 검증 및 ERD 완성

### 제품 감지 및 분류 결과

- 테이블명을 `products`에서 `product_detections`로 변경했습니다.
- 기본 키를 `product_id`에서 `product_detection_id`로 변경했습니다.
- 분류 실패를 저장할 수 있도록 `product_type_id`, `confidence`, `image_path`에 `NULL`을 허용했습니다.
- 신뢰도를 `DECIMAL(5,4)`로 변경했습니다.
- 성공 시 제품 유형과 신뢰도가 필요하고, 실패 시 제품 유형이 없어야 한다는 제약조건을 추가했습니다.

### 알림 테이블

- 발생 작업자용 `user_id`를 제거했습니다.
- 알림 발생 당시 작업자는 `alerts → production_sessions → users` 관계로 조회하도록 했습니다.
- 알림 확인 사용자만 `checked_by_user_id`로 직접 저장하도록 역할을 분리했습니다.
- 컬럼명을 다음과 같이 명확하게 변경했습니다.
  - `severity` → `priority`
  - `result_status` → `recovery_status`
  - `status` → `check_status`
  - `message` → `alert_message`
  - `acknowledged_by_user_id` → `checked_by_user_id`
  - `acknowledged_at` → `checked_at`
- 복구 시각을 저장하는 `recovered_at`을 추가했습니다.
- `INFO`는 복구 및 확인 대상이 아니므로 관련 컬럼을 `NULL`로 저장하도록 했습니다.
- 알림 유형과 중요도의 허용 조합을 제약조건으로 정의했습니다.
- 복구 상태와 복구 시각, 확인 상태와 확인 사용자·확인 시각이 일치하도록 검증 조건을 추가했습니다.

### 생산 작업 정합성 검증

- 목표 수량과 생산 수량이 음수가 되지 않도록 했습니다.
- 초콜릿과 사탕의 목표량이 모두 0인 작업 생성을 방지했습니다.
- 진행·일시정지 상태에는 종료 시각이 없어야 하고, 완료·취소 상태에는 종료 시각이 필요하도록 했습니다.

### 초기·더미 데이터

- 제품 유형 2종과 시스템 구성요소 14종의 기준 데이터를 작성했습니다.
- DB 구조 확인을 위한 사용자, 생산 작업, 제품 분류 결과, 알림 더미 데이터를 작성했습니다.
- 더미 사용자 비밀번호는 로그인 구현 후 BCrypt 해시로 교체할 예정입니다.

### ERD 작성

- DBeaver에서 실제 MySQL 외래키를 기반으로 편집용 `.erd` 파일을 작성했습니다.
- ERDCloud에서 논리명, 물리명, PK, FK, NULL 여부와 상태값 메모를 포함한 문서용 ERD를 작성했습니다.
- 테이블 역할에 따라 다음 색상을 적용했습니다.
  - 사용자: 파랑
  - 생산 작업: 초록
  - 제품 감지 결과: 보라
  - 제품 유형: 주황
  - 시스템 구성요소: 청록
  - 알림: 빨강

---

## 현재 완료 상태

- [x] 데이터베이스 생성 스크립트
- [x] 6개 테이블 정의
- [x] 기본 키·외래키·고유 키 정의
- [x] NULL 허용 여부 정의
- [x] CHECK 제약조건 정의
- [x] 제품 유형 초기 데이터
- [x] 시스템 구성요소 초기 데이터
- [x] 테스트용 더미 데이터
- [x] DBeaver ERD
- [x] ERDCloud ERD
- [ ] ASP.NET Core Web API 프로젝트 생성
- [ ] Entity Framework Core 모델 작성
- [ ] 로그인 및 권한 API
- [ ] 생산 작업 API
- [ ] 제품 감지 결과 API
- [ ] 시스템 구성요소 상태 API
- [ ] 알림 API
- [ ] MQTT 연동
- [ ] 관리자 대시보드 통계 API

---

## 다음 작업

1. ASP.NET Core Web API 프로젝트를 생성합니다.
2. MySQL 연결 정보를 환경별 설정 파일로 분리합니다.
3. 테이블 구조에 맞는 Entity 모델과 `DbContext`를 작성합니다.
4. 사용자 로그인과 JWT 인증부터 순서대로 API를 구현합니다.
5. 제품 분류 프로그램 및 MQTT 메시지 형식을 서버 API와 맞춥니다.
