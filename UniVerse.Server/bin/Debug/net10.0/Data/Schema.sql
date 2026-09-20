-- ==============================================================================
-- UniVerse C# ASP.NET Core Solution - Database Schema DDL
-- Provider: ADO.NET with SQLite
-- ==============================================================================

CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    full_name TEXT NOT NULL,
    enrollment_number TEXT,
    role TEXT NOT NULL DEFAULT 'student',
    hostel_name TEXT,
    room_number TEXT,
    phone_number TEXT,
    is_active_runner INTEGER NOT NULL DEFAULT 0,
    reward_balance REAL NOT NULL DEFAULT 0.0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS delivery_requests (
    id TEXT PRIMARY KEY,
    requester_id TEXT NOT NULL,
    runner_id TEXT,
    pickup_location TEXT NOT NULL,
    dropoff_location TEXT NOT NULL,
    instructions TEXT,
    total_estimated_amount REAL NOT NULL DEFAULT 0.0,
    delivery_fee REAL NOT NULL DEFAULT 0.0,
    status TEXT NOT NULL DEFAULT 'pending',
    delivery_otp TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (requester_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (runner_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS request_items (
    id TEXT PRIMARY KEY,
    request_id TEXT NOT NULL,
    name TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    notes TEXT,
    estimated_price REAL NOT NULL DEFAULT 0.0,
    FOREIGN KEY (request_id) REFERENCES delivery_requests(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS marketplace_listings (
    id TEXT PRIMARY KEY,
    seller_id TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL,
    condition TEXT NOT NULL,
    price REAL NOT NULL,
    original_price REAL,
    negotiable INTEGER NOT NULL DEFAULT 1,
    pickup_location TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'active',
    image_url TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (seller_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS marketplace_offers (
    id TEXT PRIMARY KEY,
    listing_id TEXT NOT NULL,
    buyer_id TEXT NOT NULL,
    offer_price REAL NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    created_at TEXT NOT NULL,
    FOREIGN KEY (listing_id) REFERENCES marketplace_listings(id) ON DELETE CASCADE,
    FOREIGN KEY (buyer_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Indexing for performance
CREATE INDEX IF NOT EXISTS idx_requests_status ON delivery_requests(status);
CREATE INDEX IF NOT EXISTS idx_requests_requester ON delivery_requests(requester_id);
CREATE INDEX IF NOT EXISTS idx_requests_runner ON delivery_requests(runner_id);
CREATE INDEX IF NOT EXISTS idx_marketplace_status ON marketplace_listings(status);
CREATE INDEX IF NOT EXISTS idx_marketplace_seller ON marketplace_listings(seller_id);
CREATE INDEX IF NOT EXISTS idx_offers_listing ON marketplace_offers(listing_id);
