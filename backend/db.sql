CREATE TABLE app_user (
    user_id VARCHAR(255) PRIMARY KEY,
    user_name VARCHAR(100),
    profile_image VARCHAR(255),
    created_at TIMESTAMPTZ,
    is_temp BOOLEAN
);

CREATE TABLE email_direction (
    direction_id SERIAL PRIMARY KEY,
    direction_name VARCHAR(50)
);

CREATE TABLE email_label (
    label_id SERIAL PRIMARY KEY,
    label_name VARCHAR(100)
);

CREATE TABLE token (
    token_id SERIAL PRIMARY KEY,
    provider TEXT NOT NULL,
    access_token TEXT,
    refresh_token TEXT,
    expires_at TIMESTAMPTZ,
    user_id VARCHAR(255) NOT NULL,
    CONSTRAINT FK_token_app_user FOREIGN KEY(user_id) REFERENCES app_user(user_id) ON DELETE CASCADE
);

CREATE TABLE email (
    email_id VARCHAR(255) PRIMARY KEY,
    user_id VARCHAR(255),
    from_address VARCHAR(255),
    to_address VARCHAR(255),
    received_date TIMESTAMPTZ,
    sent_date TIMESTAMPTZ,
    subject VARCHAR(255),
    body TEXT,
    direction_id INT NOT NULL,
    label_id INT,
    history_id VARCHAR(255),
    snippet VARCHAR(255),
    plain_text TEXT,
    CONSTRAINT FK_email_direction FOREIGN KEY(direction_id) REFERENCES email_direction(direction_id) ON DELETE CASCADE,
    CONSTRAINT FK_email_label FOREIGN KEY(label_id) REFERENCES email_label(label_id),
    CONSTRAINT FK_email_user FOREIGN KEY(user_id) REFERENCES app_user(user_id)
);

INSERT INTO email_direction (direction_id, direction_name) VALUES
(1, 'INBOX'),
(2, 'SENT'),
(3, 'DRAFT');

CREATE INDEX email_direction_id_index ON email(direction_id);
CREATE INDEX email_label_id_index ON email(label_id);
CREATE INDEX IX_email_user_id ON email(user_id);
CREATE INDEX IX_token_user_id ON token(user_id);
