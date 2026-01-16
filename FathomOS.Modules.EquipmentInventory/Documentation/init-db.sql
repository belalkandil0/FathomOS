-- S7 Fathom Equipment & Inventory Management System
-- Database Initialization Script for PostgreSQL
-- Version: 1.0

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- For text search

-- =====================================================
-- ORGANIZATION & LOCATIONS
-- =====================================================

CREATE TABLE companies (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    website VARCHAR(200),
    logo_url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    updated_by UUID
);

CREATE TABLE regions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE location_types (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL,
    icon VARCHAR(50),
    color VARCHAR(7),
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE locations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    region_id UUID REFERENCES regions(id),
    parent_location_id UUID REFERENCES locations(id),
    location_type_id UUID REFERENCES location_types(id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    address TEXT,
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    contact_person VARCHAR(100),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(100),
    is_offshore BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    capacity INT,
    qr_code VARCHAR(100),
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE vessels (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    location_id UUID REFERENCES locations(id),
    imo_number VARCHAR(20),
    call_sign VARCHAR(20),
    flag VARCHAR(50),
    vessel_type VARCHAR(50),
    gross_tonnage DECIMAL(12, 2),
    length DECIMAL(8, 2),
    beam DECIMAL(8, 2),
    draft DECIMAL(8, 2),
    owner_company VARCHAR(200),
    operator_company VARCHAR(200),
    classification_society VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- USER MANAGEMENT & AUTHENTICATION
-- =====================================================

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    is_system_role BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(50),
    description TEXT
);

CREATE TABLE role_permissions (
    role_id UUID REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(200) UNIQUE NOT NULL,
    password_hash VARCHAR(500),
    salt VARCHAR(100),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(50),
    pin VARCHAR(200),
    pin_salt VARCHAR(50),
    profile_photo_url VARCHAR(500),
    default_location_id UUID REFERENCES locations(id),
    is_ad_user BOOLEAN DEFAULT FALSE,
    ad_object_id VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at TIMESTAMP,
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMP,
    must_change_password BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE user_roles (
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT NOW(),
    assigned_by UUID REFERENCES users(id),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE user_locations (
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    location_id UUID REFERENCES locations(id) ON DELETE CASCADE,
    access_level VARCHAR(20),
    PRIMARY KEY (user_id, location_id)
);

CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) NOT NULL,
    device_info VARCHAR(200),
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    revoked_at TIMESTAMP
);

-- =====================================================
-- EQUIPMENT & INVENTORY
-- =====================================================

CREATE TABLE equipment_categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_category_id UUID REFERENCES equipment_categories(id),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    color VARCHAR(7),
    is_consumable BOOLEAN DEFAULT FALSE,
    requires_certification BOOLEAN DEFAULT FALSE,
    requires_calibration BOOLEAN DEFAULT FALSE,
    default_certification_period_days INT,
    default_calibration_period_days INT,
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE equipment_types (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    category_id UUID REFERENCES equipment_categories(id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    manufacturer VARCHAR(200),
    model VARCHAR(200),
    default_unit VARCHAR(20),
    specifications_json JSONB,
    is_active BOOLEAN DEFAULT TRUE,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE suppliers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE,
    contact_person VARCHAR(100),
    email VARCHAR(200),
    phone VARCHAR(50),
    address TEXT,
    website VARCHAR(200),
    tax_id VARCHAR(50),
    payment_terms VARCHAR(100),
    notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id UUID REFERENCES companies(id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    client_name VARCHAR(200),
    description TEXT,
    location_id UUID REFERENCES locations(id),
    vessel_id UUID REFERENCES vessels(id),
    start_date DATE,
    end_date DATE,
    status VARCHAR(20) DEFAULT 'Active',
    project_manager VARCHAR(100),
    contact_email VARCHAR(200),
    contact_phone VARCHAR(50),
    budget DECIMAL(15, 2),
    currency VARCHAR(3) DEFAULT 'USD',
    notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE equipment (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_number VARCHAR(50) UNIQUE NOT NULL,
    sap_number VARCHAR(50),
    tech_number VARCHAR(50),
    serial_number VARCHAR(100),
    qr_code VARCHAR(100) UNIQUE,
    barcode VARCHAR(100),
    type_id UUID REFERENCES equipment_types(id),
    category_id UUID REFERENCES equipment_categories(id),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    manufacturer VARCHAR(200),
    model VARCHAR(200),
    part_number VARCHAR(100),
    specifications JSONB,
    weight_kg DECIMAL(10, 3),
    length_cm DECIMAL(10, 2),
    width_cm DECIMAL(10, 2),
    height_cm DECIMAL(10, 2),
    volume_cm3 DECIMAL(15, 2),
    packaging_type VARCHAR(50),
    packaging_weight_kg DECIMAL(10, 3),
    packaging_length_cm DECIMAL(10, 2),
    packaging_width_cm DECIMAL(10, 2),
    packaging_height_cm DECIMAL(10, 2),
    packaging_description TEXT,
    current_location_id UUID REFERENCES locations(id),
    current_project_id UUID REFERENCES projects(id),
    current_custodian_id UUID REFERENCES users(id),
    status VARCHAR(30) DEFAULT 'Available',
    condition VARCHAR(20) DEFAULT 'Good',
    ownership_type VARCHAR(20) DEFAULT 'Owned',
    supplier_id UUID REFERENCES suppliers(id),
    purchase_date DATE,
    purchase_price DECIMAL(15, 2),
    purchase_currency VARCHAR(3) DEFAULT 'USD',
    purchase_order_number VARCHAR(50),
    warranty_expiry_date DATE,
    rental_start_date DATE,
    rental_end_date DATE,
    rental_rate DECIMAL(15, 2),
    rental_rate_period VARCHAR(20),
    depreciation_method VARCHAR(20),
    useful_life_years INT,
    residual_value DECIMAL(15, 2),
    current_value DECIMAL(15, 2),
    requires_certification BOOLEAN DEFAULT FALSE,
    certification_number VARCHAR(100),
    certification_body VARCHAR(200),
    certification_date DATE,
    certification_expiry_date DATE,
    requires_calibration BOOLEAN DEFAULT FALSE,
    last_calibration_date DATE,
    next_calibration_date DATE,
    calibration_interval INT,
    last_service_date DATE,
    next_service_date DATE,
    service_interval INT,
    last_inspection_date DATE,
    is_consumable BOOLEAN DEFAULT FALSE,
    quantity_on_hand DECIMAL(15, 3) DEFAULT 1,
    unit_of_measure VARCHAR(20) DEFAULT 'Each',
    minimum_stock_level DECIMAL(15, 3),
    reorder_level DECIMAL(15, 3),
    maximum_stock_level DECIMAL(15, 3),
    batch_number VARCHAR(50),
    lot_number VARCHAR(50),
    expiry_date DATE,
    is_permanent_equipment BOOLEAN DEFAULT FALSE,
    is_project_equipment BOOLEAN DEFAULT FALSE,
    primary_photo_url VARCHAR(500),
    qr_code_image_url VARCHAR(500),
    notes TEXT,
    internal_notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id)
);

CREATE TABLE equipment_photos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    equipment_id UUID REFERENCES equipment(id) ON DELETE CASCADE,
    photo_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500),
    caption VARCHAR(200),
    photo_type VARCHAR(20),
    taken_at TIMESTAMP,
    taken_by UUID REFERENCES users(id),
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE equipment_documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    equipment_id UUID REFERENCES equipment(id) ON DELETE CASCADE,
    document_url VARCHAR(500) NOT NULL,
    file_name VARCHAR(200),
    file_type VARCHAR(50),
    file_size_bytes BIGINT,
    document_type VARCHAR(50),
    description TEXT,
    expiry_date DATE,
    uploaded_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE equipment_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    equipment_id UUID REFERENCES equipment(id),
    event_type VARCHAR(50) NOT NULL,
    event_description TEXT,
    previous_value JSONB,
    new_value JSONB,
    from_location_id UUID REFERENCES locations(id),
    to_location_id UUID REFERENCES locations(id),
    manifest_id UUID,
    performed_by UUID REFERENCES users(id),
    performed_at TIMESTAMP DEFAULT NOW(),
    notes TEXT,
    sync_version BIGINT DEFAULT 0
);

-- =====================================================
-- MANIFESTS
-- =====================================================

CREATE TABLE manifests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    manifest_number VARCHAR(50) UNIQUE NOT NULL,
    qr_code VARCHAR(100) UNIQUE,
    manifest_type VARCHAR(10) NOT NULL,
    from_location_id UUID REFERENCES locations(id),
    from_contact_name VARCHAR(100),
    from_contact_phone VARCHAR(50),
    from_contact_email VARCHAR(200),
    to_location_id UUID REFERENCES locations(id),
    to_contact_name VARCHAR(100),
    to_contact_phone VARCHAR(50),
    to_contact_email VARCHAR(200),
    project_id UUID REFERENCES projects(id),
    status VARCHAR(20) DEFAULT 'Draft',
    created_date TIMESTAMP DEFAULT NOW(),
    submitted_date TIMESTAMP,
    approved_date TIMESTAMP,
    shipped_date TIMESTAMP,
    expected_arrival_date DATE,
    received_date TIMESTAMP,
    completed_date TIMESTAMP,
    shipping_method VARCHAR(50),
    carrier_name VARCHAR(200),
    tracking_number VARCHAR(100),
    vehicle_number VARCHAR(50),
    driver_name VARCHAR(100),
    driver_phone VARCHAR(50),
    total_items INT DEFAULT 0,
    total_weight DECIMAL(12, 3),
    total_volume DECIMAL(15, 2),
    created_by UUID REFERENCES users(id),
    submitted_by UUID REFERENCES users(id),
    approved_by UUID REFERENCES users(id),
    rejected_by UUID REFERENCES users(id),
    shipped_by UUID REFERENCES users(id),
    received_by UUID REFERENCES users(id),
    sender_signature TEXT,
    sender_signed_at TIMESTAMP,
    receiver_signature TEXT,
    receiver_signed_at TIMESTAMP,
    approver_signature TEXT,
    approver_signed_at TIMESTAMP,
    notes TEXT,
    internal_notes TEXT,
    rejection_reason TEXT,
    has_discrepancies BOOLEAN DEFAULT FALSE,
    discrepancy_notes TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE manifest_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    manifest_id UUID REFERENCES manifests(id) ON DELETE CASCADE,
    equipment_id UUID REFERENCES equipment(id),
    asset_number VARCHAR(50),
    name VARCHAR(200),
    description TEXT,
    serial_number VARCHAR(100),
    quantity DECIMAL(15, 3) DEFAULT 1,
    unit_of_measure VARCHAR(20) DEFAULT 'Each',
    weight_kg DECIMAL(10, 3),
    condition_at_send VARCHAR(20),
    condition_notes TEXT,
    is_received BOOLEAN DEFAULT FALSE,
    received_quantity DECIMAL(15, 3),
    received_date TIMESTAMP,
    received_by UUID REFERENCES users(id),
    condition_at_receive VARCHAR(20),
    receipt_notes TEXT,
    has_discrepancy BOOLEAN DEFAULT FALSE,
    discrepancy_type VARCHAR(20),
    discrepancy_notes TEXT,
    send_photo_url VARCHAR(500),
    receive_photo_url VARCHAR(500),
    sort_order INT DEFAULT 0,
    sync_version BIGINT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE manifest_photos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    manifest_id UUID REFERENCES manifests(id) ON DELETE CASCADE,
    item_id UUID REFERENCES manifest_items(id) ON DELETE CASCADE,
    photo_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500),
    photo_type VARCHAR(20),
    caption VARCHAR(200),
    taken_at TIMESTAMP,
    taken_by UUID REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE manifest_approvals (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    manifest_id UUID REFERENCES manifests(id) ON DELETE CASCADE,
    approval_level INT DEFAULT 1,
    status VARCHAR(20),
    approver_id UUID REFERENCES users(id),
    approved_at TIMESTAMP,
    comments TEXT,
    signature TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- SYNC & AUDIT
-- =====================================================

CREATE TABLE sync_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    device_id VARCHAR(100) NOT NULL,
    user_id UUID REFERENCES users(id),
    sync_type VARCHAR(20),
    started_at TIMESTAMP DEFAULT NOW(),
    completed_at TIMESTAMP,
    status VARCHAR(20),
    records_uploaded INT DEFAULT 0,
    records_downloaded INT DEFAULT 0,
    conflicts_found INT DEFAULT 0,
    conflicts_resolved INT DEFAULT 0,
    error_message TEXT,
    sync_version BIGINT
);

CREATE TABLE sync_conflicts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(100) NOT NULL,
    record_id UUID NOT NULL,
    device_id VARCHAR(100),
    user_id UUID REFERENCES users(id),
    local_data JSONB,
    server_data JSONB,
    conflict_type VARCHAR(20),
    resolution VARCHAR(20),
    resolved_by UUID REFERENCES users(id),
    resolved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE offline_queues (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    device_id VARCHAR(100) NOT NULL,
    user_id UUID REFERENCES users(id),
    table_name VARCHAR(100) NOT NULL,
    record_id UUID NOT NULL,
    operation VARCHAR(20),
    data JSONB,
    priority INT DEFAULT 0,
    attempts INT DEFAULT 0,
    last_attempt TIMESTAMP,
    error_message TEXT,
    status VARCHAR(20) DEFAULT 'Pending',
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(100),
    record_id UUID,
    action VARCHAR(20),
    user_id UUID REFERENCES users(id),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(50),
    user_agent VARCHAR(500),
    device_id VARCHAR(100),
    timestamp TIMESTAMP DEFAULT NOW()
);

CREATE TABLE alerts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    alert_type VARCHAR(50) NOT NULL,
    equipment_id UUID REFERENCES equipment(id),
    manifest_id UUID REFERENCES manifests(id),
    location_id UUID REFERENCES locations(id),
    title VARCHAR(200),
    message TEXT,
    severity VARCHAR(20),
    due_date DATE,
    is_acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_by UUID REFERENCES users(id),
    acknowledged_at TIMESTAMP,
    is_resolved BOOLEAN DEFAULT FALSE,
    resolved_by UUID REFERENCES users(id),
    resolved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- INDEXES
-- =====================================================

CREATE INDEX idx_equipment_location ON equipment(current_location_id);
CREATE INDEX idx_equipment_project ON equipment(current_project_id);
CREATE INDEX idx_equipment_status ON equipment(status);
CREATE INDEX idx_equipment_category ON equipment(category_id);
CREATE INDEX idx_equipment_type ON equipment(type_id);
CREATE INDEX idx_equipment_qr ON equipment(qr_code);
CREATE INDEX idx_equipment_asset ON equipment(asset_number);
CREATE INDEX idx_equipment_serial ON equipment(serial_number);
CREATE INDEX idx_equipment_cert_expiry ON equipment(certification_expiry_date);
CREATE INDEX idx_equipment_calibration ON equipment(next_calibration_date);
CREATE INDEX idx_equipment_sync ON equipment(sync_version);
CREATE INDEX idx_equipment_name_trgm ON equipment USING gin(name gin_trgm_ops);

CREATE INDEX idx_manifest_status ON manifests(status);
CREATE INDEX idx_manifest_type ON manifests(manifest_type);
CREATE INDEX idx_manifest_from ON manifests(from_location_id);
CREATE INDEX idx_manifest_to ON manifests(to_location_id);
CREATE INDEX idx_manifest_qr ON manifests(qr_code);
CREATE INDEX idx_manifest_sync ON manifests(sync_version);

CREATE INDEX idx_history_equipment ON equipment_history(equipment_id);
CREATE INDEX idx_history_date ON equipment_history(performed_at);

CREATE INDEX idx_audit_table ON audit_logs(table_name, record_id);
CREATE INDEX idx_audit_user ON audit_logs(user_id);
CREATE INDEX idx_audit_time ON audit_logs(timestamp);

CREATE INDEX idx_location_sync ON locations(sync_version);
CREATE INDEX idx_category_sync ON equipment_categories(sync_version);
CREATE INDEX idx_type_sync ON equipment_types(sync_version);
CREATE INDEX idx_project_sync ON projects(sync_version);

-- =====================================================
-- SEED DATA
-- =====================================================

-- Default Roles
INSERT INTO roles (id, name, description, is_system_role) VALUES
    ('00000000-0000-0000-0000-000000000001', 'System Administrator', 'Full system access', TRUE),
    ('00000000-0000-0000-0000-000000000002', 'Base Manager', 'Manage onshore base operations', TRUE),
    ('00000000-0000-0000-0000-000000000003', 'Vessel Superintendent', 'Manage vessel equipment', TRUE),
    ('00000000-0000-0000-0000-000000000004', 'Project Manager', 'View and approve project transfers', TRUE),
    ('00000000-0000-0000-0000-000000000005', 'Deck Operator', 'Scan and create manifests', TRUE),
    ('00000000-0000-0000-0000-000000000006', 'Store Keeper', 'Manage inventory', TRUE),
    ('00000000-0000-0000-0000-000000000007', 'Auditor', 'Read-only access for auditing', TRUE);

-- Default Permissions
INSERT INTO permissions (id, name, category, description) VALUES
    -- Equipment
    ('10000000-0000-0000-0000-000000000001', 'equipment.view', 'Equipment', 'View equipment'),
    ('10000000-0000-0000-0000-000000000002', 'equipment.create', 'Equipment', 'Create new equipment'),
    ('10000000-0000-0000-0000-000000000003', 'equipment.edit', 'Equipment', 'Edit equipment'),
    ('10000000-0000-0000-0000-000000000004', 'equipment.delete', 'Equipment', 'Delete equipment'),
    ('10000000-0000-0000-0000-000000000005', 'equipment.export', 'Equipment', 'Export equipment data'),
    ('10000000-0000-0000-0000-000000000006', 'equipment.import', 'Equipment', 'Import equipment data'),
    ('10000000-0000-0000-0000-000000000007', 'equipment.qr.generate', 'Equipment', 'Generate QR codes'),
    -- Manifests
    ('20000000-0000-0000-0000-000000000001', 'manifest.view', 'Manifest', 'View manifests'),
    ('20000000-0000-0000-0000-000000000002', 'manifest.create', 'Manifest', 'Create manifests'),
    ('20000000-0000-0000-0000-000000000003', 'manifest.edit', 'Manifest', 'Edit manifests'),
    ('20000000-0000-0000-0000-000000000004', 'manifest.delete', 'Manifest', 'Delete manifests'),
    ('20000000-0000-0000-0000-000000000005', 'manifest.approve', 'Manifest', 'Approve manifests'),
    ('20000000-0000-0000-0000-000000000006', 'manifest.receive', 'Manifest', 'Receive manifests'),
    ('20000000-0000-0000-0000-000000000007', 'manifest.export', 'Manifest', 'Export manifests'),
    -- Reports
    ('30000000-0000-0000-0000-000000000001', 'reports.view', 'Reports', 'View reports'),
    ('30000000-0000-0000-0000-000000000002', 'reports.generate', 'Reports', 'Generate reports'),
    ('30000000-0000-0000-0000-000000000003', 'reports.export', 'Reports', 'Export reports'),
    -- Admin
    ('40000000-0000-0000-0000-000000000001', 'admin.users', 'Admin', 'Manage users'),
    ('40000000-0000-0000-0000-000000000002', 'admin.roles', 'Admin', 'Manage roles'),
    ('40000000-0000-0000-0000-000000000003', 'admin.locations', 'Admin', 'Manage locations'),
    ('40000000-0000-0000-0000-000000000004', 'admin.settings', 'Admin', 'System settings'),
    ('40000000-0000-0000-0000-000000000005', 'admin.audit', 'Admin', 'View audit logs');

-- System Admin gets all permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT '00000000-0000-0000-0000-000000000001', id FROM permissions;

-- Default Location Types
INSERT INTO location_types (id, name, icon, color) VALUES
    ('01000000-0000-0000-0000-000000000001', 'Base', 'Warehouse', '#4CAF50'),
    ('01000000-0000-0000-0000-000000000002', 'Vessel', 'Ship', '#2196F3'),
    ('01000000-0000-0000-0000-000000000003', 'Project Site', 'Construction', '#FF9800'),
    ('01000000-0000-0000-0000-000000000004', 'Container', 'Package', '#9C27B0'),
    ('01000000-0000-0000-0000-000000000005', 'Storage Yard', 'Grid', '#607D8B'),
    ('01000000-0000-0000-0000-000000000006', 'Workshop', 'Wrench', '#795548');

-- Default Equipment Categories
INSERT INTO equipment_categories (id, name, code, icon, requires_certification, requires_calibration, is_consumable) VALUES
    ('02000000-0000-0000-0000-000000000001', 'Survey Equipment', 'SURV', 'Radar', TRUE, TRUE, FALSE),
    ('02000000-0000-0000-0000-000000000002', 'Lifting Equipment', 'LIFT', 'Crane', TRUE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000003', 'Safety Equipment', 'SAFE', 'Shield', TRUE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000004', 'Tools', 'TOOL', 'Hammer', FALSE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000005', 'Electronics', 'ELEC', 'Cpu', FALSE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000006', 'Consumables', 'CONS', 'Package', FALSE, FALSE, TRUE),
    ('02000000-0000-0000-0000-000000000007', 'Containers & Cases', 'CONT', 'Box', FALSE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000008', 'ROV Equipment', 'ROV', 'Submarine', TRUE, TRUE, FALSE),
    ('02000000-0000-0000-0000-000000000009', 'Diving Equipment', 'DIVE', 'Waves', TRUE, FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000010', 'Communication', 'COMM', 'Radio', FALSE, FALSE, FALSE);

-- Default Admin User (password: Admin@123)
INSERT INTO users (id, username, email, password_hash, salt, first_name, last_name, is_active) VALUES
    ('99000000-0000-0000-0000-000000000001', 'admin', 'admin@s7fathom.local', 
     'AQAAAAIAAYagAAAAEK8bJTZV8wB2kX2zLr3p5hS7qPt5B7yxN2mR5wL8dZE0Y9YPxX3nT3M2vL6HhJ1q8Q==',
     'S7F@th0m2024!', 'System', 'Administrator', TRUE);

INSERT INTO user_roles (user_id, role_id) VALUES
    ('99000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001');

-- Sample Company
INSERT INTO companies (id, name, code) VALUES
    ('03000000-0000-0000-0000-000000000001', 'S7 Fathom', 'S7F');

-- Sample Region
INSERT INTO regions (id, company_id, name, code) VALUES
    ('04000000-0000-0000-0000-000000000001', '03000000-0000-0000-0000-000000000001', 'Middle East', 'ME');

-- Sample Locations
INSERT INTO locations (id, region_id, location_type_id, name, code, is_offshore) VALUES
    ('05000000-0000-0000-0000-000000000001', '04000000-0000-0000-0000-000000000001', '01000000-0000-0000-0000-000000000001', 'Abu Dhabi Base', 'BASE-ABU-001', FALSE),
    ('05000000-0000-0000-0000-000000000002', '04000000-0000-0000-0000-000000000001', '01000000-0000-0000-0000-000000000001', 'Dubai Base', 'BASE-DXB-001', FALSE),
    ('05000000-0000-0000-0000-000000000003', '04000000-0000-0000-0000-000000000001', '01000000-0000-0000-0000-000000000002', 'MV Explorer', 'VES-EXP-001', TRUE),
    ('05000000-0000-0000-0000-000000000004', '04000000-0000-0000-0000-000000000001', '01000000-0000-0000-0000-000000000002', 'MV Discovery', 'VES-DIS-001', TRUE);

-- Grant admin access to all locations
INSERT INTO user_locations (user_id, location_id, access_level)
SELECT '99000000-0000-0000-0000-000000000001', id, 'Admin' FROM locations;

COMMIT;
