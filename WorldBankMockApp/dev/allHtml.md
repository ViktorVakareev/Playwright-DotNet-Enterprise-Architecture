--- login.html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Authentication</title>
    
    <!-- Added missing link to Universal Nav CSS -->
    <link rel="stylesheet" href="styles.css">
    
    <style>
        :root { --primary: #002244; --error: #d32f2f; --bg: #f4f7f6; }
        
        /* FIXED: Body is now a vertical flex column so the header sits at the very top */
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; background-color: var(--bg); display: flex; flex-direction: column; min-height: 100vh; }
        
        /* NEW: Container to hold the split view taking up the remaining space below the header */
        .login-container { display: flex; flex: 1; }
        
        .split-left { flex: 1; background: url('https://images.unsplash.com/photo-1554224155-6726b3ff858f?q=80&w=2026&auto=format&fit=crop') center/cover; }
        .split-right { flex: 1; display: flex; align-items: center; justify-content: center; background: white; box-shadow: -5px 0 15px rgba(0,0,0,0.05); }
        .login-card { width: 100%; max-width: 400px; padding: 40px; }
        h1 { color: var(--primary); font-size: 1.8rem; margin-bottom: 30px; }
        .error { background: #ffebee; color: var(--error); padding: 12px; border-left: 4px solid var(--error); border-radius: 4px; display: none; margin-bottom: 20px; font-weight: 500; font-size: 0.9rem; }
        .form-group { margin-bottom: 20px; }
        .form-group label { display: block; margin-bottom: 8px; color: #555; font-weight: 600; font-size: 0.9rem; }
        input[type="text"], input[type="password"] { width: 100%; padding: 12px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; font-size: 1rem; transition: border-color 0.3s; }
        input[type="text"]:focus, input[type="password"]:focus { border-color: var(--primary); outline: none; box-shadow: 0 0 0 2px rgba(0,34,68,0.1); }
        button { width: 100%; background-color: var(--primary); color: white; border: none; padding: 14px; border-radius: 4px; font-size: 1rem; font-weight: bold; cursor: pointer; transition: background 0.3s; }
        button:hover { background-color: #003366; }
        #mfa-section { display: none; background: #fff8e1; padding: 20px; border: 1px solid #ffc107; border-radius: 4px; margin-top: 20px; }
        .back-link { display: block; text-align: center; margin-top: 20px; color: #666; text-decoration: none; font-size: 0.9rem; }
        .back-link:hover { text-decoration: underline; }
    </style>
</head>
<body>

    <!-- UNIVERSAL NAVIGATION HEADER -->
    <header class="universal-header">
        <div class="header-top">
            <div class="brand-section">
                <a href="index.html" class="logo-text">WorldBank QA</a>
                <div class="pill-toggle">
                    <a href="index.html" class="active" data-testid="toggle-public">Public Sandbox</a>
                    <a href="dashboard.html" data-testid="toggle-secure">Secure Sandbox</a>
                </div>
            </div>
            <div class="header-utilities">
                <a href="#">About</a>
                <a href="#">Support 24/7</a>
                <a href="#">Contacts</a>
                <a href="#">EN 🌐</a>
            </div>
        </div>
        <div class="header-bottom">
            <div class="main-nav-links">
                <a href="index.html" data-testid="nav-home">Home</a>
                <a href="dashboard.html" data-testid="nav-dashboard">Dashboard</a>
                <a href="transfer.html" data-testid="nav-transfer">Wire Transfer</a>
                <a href="settings.html" data-testid="nav-settings">Settings</a>
            </div>
            <div>
                <a href="login.html" class="action-btn" data-testid="nav-login-btn">Secure Login / Logout</a>
            </div>
        </div>
    </header>

    <!-- NEW WRAPPER FOR THE SPLIT SCREEN -->
    <div class="login-container">
        <div class="split-left"></div>
        <div class="split-right">
            <div class="login-card">
                <h1>Authentication Gateway</h1>
                <div id="error-message" class="error" data-testid="error-message"></div>
                
                <form id="login-form">
                    <div class="form-group">
                        <label for="username">Corporate ID</label>
                        <input type="text" id="username" data-testid="input-username" placeholder="Username" required>
                    </div>
                    <div class="form-group">
                        <label for="password">Security Token</label>
                        <input type="password" id="password" data-testid="input-password" placeholder="Password" required>
                    </div>
                    <button type="submit" id="login-button" data-testid="btn-login-submit">Secure Login</button>
                </form>

                <div id="mfa-section" data-testid="mfa-section">
                    <h3 style="margin-top:0; color:#d84315;">MFA Challenge</h3>
                    <p style="font-size: 0.9rem; color: #555;">Please verify your new device.</p>
                    <div class="form-group">
                        <input type="text" id="mfa-code" data-testid="input-mfa-code" placeholder="Enter 6-digit code">
                    </div>
                    <button id="verify-mfa" data-testid="btn-verify-mfa">Verify Device</button>
                </div>
                
                <a href="index.html" class="back-link" data-testid="link-back-home">&larr; Back to Public Site</a>
            </div>
        </div>
    </div>

    <script>
        let failedAttempts = 0;
        document.getElementById('login-form').addEventListener('submit', (e) => {
            e.preventDefault();
            const u = document.getElementById('username').value;
            const p = document.getElementById('password').value;
            const err = document.getElementById('error-message');
            
            if (failedAttempts >= 4) {
                err.innerText = "Account Locked due to too many failed attempts.";
                err.style.display = "block"; return;
            }
            if (u.includes("' OR") || u.includes("<script>")) {
                err.innerText = "Security Violation: Invalid Input Detected";
                err.style.display = "block"; return;
            }
            if (u === "newdevice_user") {
                document.getElementById('mfa-section').style.display = "block";
                document.getElementById('login-form').style.display = "none"; return;
            }
            if (u === "standarduser" && p === "password123") {
                document.cookie = "session=secure_token; HttpOnly; Secure";
                window.location.href = "dashboard.html?role=standard"; return;
            }
            failedAttempts++;
            err.innerText = "Invalid credentials. Generic error.";
            err.style.display = "block";
        });
    </script>
</body>
</html>
--- index.html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>World Bank Sandbox - Home</title>
    
    <!-- This link is critical! It pulls in the Universal Nav CSS -->
    <link rel="stylesheet" href="styles.css">
    
    <style>
        /* Specific styles just for the landing page body */
        :root { --primary: #002244; --secondary: #005596; --bg: #f4f7f6; --text: #333; }
        body { font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; background-color: var(--bg); color: var(--text); }
        .hero { background: linear-gradient(rgba(0, 34, 68, 0.8), rgba(0, 34, 68, 0.8)), url('https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?q=80&w=2070&auto=format&fit=crop') no-repeat center center; background-size: cover; color: white; text-align: center; padding: 100px 20px; }
        .search-container { max-width: 600px; margin: -50px auto 50px auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 10px 20px rgba(0,0,0,0.1); }
        .search-container h3 { margin-top: 0; color: var(--primary); font-size: 1.2rem; border-bottom: 2px solid var(--bg); padding-bottom: 10px; }
        .input-group { display: flex; gap: 10px; margin-top: 20px; }
        input[type="text"] { flex: 1; padding: 12px; border: 1px solid #ccc; border-radius: 4px; font-size: 1rem; }
        button { background-color: var(--primary); color: white; border: none; padding: 12px 24px; border-radius: 4px; cursor: pointer; font-size: 1rem; font-weight: bold; transition: background 0.3s; }
        button:hover { background-color: var(--secondary); }
        #search-results { margin-top: 20px; padding: 15px; border-radius: 4px; background: var(--bg); min-height: 20px; }
        .result-item { display: block; padding: 10px; background: white; border-left: 4px solid var(--secondary); box-shadow: 0 2px 4px rgba(0,0,0,0.05); }
    </style>
</head>
<body>
    
    <!-- UNIVERSAL NAVIGATION HEADER -->
    <header class="universal-header">
        <!-- Top Bar: Brand, Environment Toggles, Utilities -->
        <div class="header-top">
            <div class="brand-section">
                <a href="index.html" class="logo-text">WorldBank QA</a>
                <div class="pill-toggle">
                    <a href="index.html" class="active" data-testid="toggle-public">Public Sandbox</a>
                    <a href="dashboard.html" data-testid="toggle-secure">Secure Sandbox</a>
                </div>
            </div>
            <div class="header-utilities">
                <a href="#">About</a>
                <a href="#">Support 24/7</a>
                <a href="#">Contacts</a>
                <a href="#">EN 🌐</a>
            </div>
        </div>

        <!-- Bottom Bar: Main Navigation Links & Action Button -->
        <div class="header-bottom">
            <div class="main-nav-links">
                <a href="index.html" data-testid="nav-home">Home</a>
                <a href="dashboard.html" data-testid="nav-dashboard">Dashboard</a>
                <a href="transfer.html" data-testid="nav-transfer">Wire Transfer</a>
                <a href="settings.html" data-testid="nav-settings">Settings</a>
            </div>
            <div>
                <a href="login.html" class="action-btn" data-testid="nav-login-btn">Secure Login / Logout</a>
            </div>
        </div>
    </header>

    <div class="hero">
        <h2>Global Financial Intelligence Network (dev)</h2>
        <p>Enterprise sandbox environment for automated QA systems.</p>
    </div>
    
    <div class="search-container">
        <h3>Directory Search</h3>
        <div class="input-group">
            <input type="text" id="search-input" data-testid="search-input" placeholder="Search employees/users...">
            <button id="search-btn" data-testid="search-btn">Search</button>
        </div>
        <div id="search-results" data-testid="search-results"></div>
    </div>

    <script>
        document.getElementById('search-btn').addEventListener('click', () => {
            const query = document.getElementById('search-input').value;
            const res = document.getElementById('search-results');
            if (query.includes("Synthetic")) {
                res.innerHTML = "<span class='result-item'>Synthetic User Profile Loaded</span>";
            } else if (query.trim() === "") {
                res.innerHTML = "No results found.";
            } else {
                res.innerHTML = "No results found.";
            }
        });
    </script>
</body>
</html>
--- dashboard.html
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <meta charset="UTF-8">
    <title>Dashboard - WorldBank Mock</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <!-- UNIVERSAL NAVIGATION HEADER -->
    <header class="universal-header">
        <div class="header-top">
            <div class="brand-section">
                <a href="index.html" class="logo-text">WorldBank QA</a>
                <div class="pill-toggle">
                    <a href="index.html" data-testid="toggle-public">Public Sandbox</a>
                    <a href="dashboard.html" class="active" data-testid="toggle-secure">Secure Sandbox</a>
                </div>
            </div>
            <div class="header-utilities">
                <a href="#">About</a>
                <a href="#">Support 24/7</a>
                <a href="#">Contacts</a>
                <a href="#">EN 🌐</a>
            </div>
        </div>
        <div class="header-bottom">
            <div class="main-nav-links">
                <a href="index.html" data-testid="nav-home">Home</a>
                <a href="dashboard.html" data-testid="nav-dashboard">Dashboard</a>
                <a href="transfer.html" data-testid="nav-transfer">Wire Transfer</a>
                <a href="settings.html" data-testid="nav-settings">Settings</a>
            </div>
            <div>
                <a href="login.html" class="action-btn" data-testid="nav-login-btn">Secure Login / Logout</a>
            </div>
        </div>
    </header>

    <!-- Overlay & Modal -->
    <div class="overlay" id="overlay"></div>
    <div class="modal" id="notification-modal" data-testid="notification-modal">
        <h3>Unread Alerts</h3>
        <p>⚠️ Login attempt from new device detected.</p>
        <button id="btn-close-modal" data-testid="btn-close-modal" style="padding: 8px 16px; cursor: pointer;">Close</button>
    </div>

    <div class="container">
        <!-- NEW PAGE-LEVEL CONTROL BAR -->
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
            <h2 data-testid="app-title" style="margin: 0; color: var(--primary);">Dashboard Overview</h2>
            
            <div class="page-actions">
                <button id="btn-dark-mode" data-testid="btn-dark-mode" style="padding: 8px 16px; margin-right: 10px; cursor: pointer; border-radius: 4px; border: 1px solid var(--border); background: var(--surface-color); color: var(--text-main);">Toggle Dark Mode</button>
                <button id="btn-notifications" data-testid="btn-notifications" style="padding: 8px 16px; cursor: pointer; border-radius: 4px; border: 1px solid var(--border); background: var(--surface-color); color: var(--text-main);">🔔 Alerts (2)</button>
            </div>
        </div>

        <div class="card">
            <h3 style="margin-top: 0;">Account Ledger</h3>
            <input type="text" id="search-ledger" data-testid="search-ledger" placeholder="Search transactions..." style="width: 100%; padding: 10px; margin-bottom: 15px; border: 1px solid var(--border); border-radius: 4px; box-sizing: border-box;">
            
            <table id="ledger-table" data-testid="ledger-table">
                <thead>
                    <tr><th>Date ↕</th><th>Description</th><th>Amount</th><th>Status</th></tr>
                </thead>
                <tbody>
                    <tr class="ledger-row" data-description="inbound wire tech llc">
                        <td>2026-05-13</td><td>Inbound Wire - Tech LLC</td><td>+$45,000.00</td><td><span class="badge bg-success">Cleared</span></td>
                    </tr>
                    <tr class="ledger-row" data-description="cloud hosting provider">
                        <td>2026-05-12</td><td>Cloud Hosting Provider</td><td>-$1,250.00</td><td><span class="badge bg-warning">Pending</span></td>
                    </tr>
                    <tr class="ledger-row" data-description="suspicious ach withdrawal">
                        <td>2026-05-10</td><td>Suspicious ACH Withdrawal</td><td>-$9,999.00</td><td><span class="badge bg-danger">Flagged</span></td>
                    </tr>
                </tbody>
            </table>
            <p id="no-results" data-testid="no-results-msg" style="display: none; color: var(--danger); margin-top: 15px;">No transactions found.</p>
        </div>
    </div>

    <script>
        // Dark Mode Logic
        document.getElementById('btn-dark-mode').addEventListener('click', () => {
            const html = document.documentElement;
            html.dataset.theme = html.dataset.theme === 'dark' ? 'light' : 'dark';
        });

        // Modal Logic
        document.getElementById('btn-notifications').addEventListener('click', () => {
            document.getElementById('notification-modal').classList.add('active');
            document.getElementById('overlay').classList.add('active');
        });
        document.getElementById('btn-close-modal').addEventListener('click', () => {
            document.getElementById('notification-modal').classList.remove('active');
            document.getElementById('overlay').classList.remove('active');
        });

        // Search Logic
        document.getElementById('search-ledger').addEventListener('input', function(e) {
            const term = e.target.value.toLowerCase();
            let visibleCount = 0;
            document.querySelectorAll('.ledger-row').forEach(row => {
                const match = row.dataset.description.includes(term);
                row.style.display = match ? '' : 'none';
                if(match) visibleCount++;
            });
            document.getElementById('no-results').style.display = visibleCount === 0 ? 'block' : 'none';
        });
    </script>
</body>
</html>
--- transfer.html
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <meta charset="UTF-8">
    <title>Wire Transfer - WorldBank Mock</title>
    <link rel="stylesheet" href="styles.css">
    <style> .error-text { color: var(--danger); font-size: 0.8rem; display: none; margin-top: -15px; margin-bottom: 10px; font-weight: 500; } </style>
</head>
<body>
    <!-- UNIVERSAL NAVIGATION HEADER -->
    <header class="universal-header">
        <div class="header-top">
            <div class="brand-section">
                <a href="index.html" class="logo-text">WorldBank QA</a>
                <div class="pill-toggle">
                    <a href="index.html" data-testid="toggle-public">Public Sandbox</a>
                    <a href="dashboard.html" class="active" data-testid="toggle-secure">Secure Sandbox</a>
                </div>
            </div>
            <div class="header-utilities">
                <a href="#">About</a>
                <a href="#">Support 24/7</a>
                <a href="#">Contacts</a>
                <a href="#">EN 🌐</a>
            </div>
        </div>
        <div class="header-bottom">
            <div class="main-nav-links">
                <a href="index.html" data-testid="nav-home">Home</a>
                <a href="dashboard.html" data-testid="nav-dashboard">Dashboard</a>
                <a href="transfer.html" data-testid="nav-transfer" style="color: var(--nav-active);">Wire Transfer</a>
                <a href="settings.html" data-testid="nav-settings">Settings</a>
            </div>
            <div>
                <a href="login.html" class="action-btn" data-testid="nav-login-btn">Secure Login / Logout</a>
            </div>
        </div>
    </header>

    <div class="container">
        <!-- PAGE-LEVEL CONTROL BAR -->
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
            <h2 data-testid="app-title" style="margin: 0; color: var(--primary);">Initiate Wire Transfer</h2>
            <button data-testid="btn-back-dashboard" onclick="window.location.href='dashboard.html'" style="padding: 8px 16px; cursor: pointer; border-radius: 4px; border: 1px solid var(--border); background: var(--surface-color); color: var(--text-main);">Cancel Transfer</button>
        </div>

        <div class="card">
            <div class="stepper">
                <div class="step active" id="step1-indicator" data-testid="step-1-ind"><div class="circle">1</div><span>Details</span></div>
                <div class="step" id="step2-indicator" data-testid="step-2-ind"><div class="circle">2</div><span>Amount</span></div>
                <div class="step" id="step3-indicator" data-testid="step-3-ind"><div class="circle">3</div><span>Review</span></div>
            </div>

            <!-- STEP 1 -->
            <div id="step1-form" data-testid="step-1-form">
                <p style="font-weight: 600; margin-bottom: 5px;">Select Recipient:</p>
                <select id="recipient-select" data-testid="recipient-select" style="width: 100%; padding: 12px; margin-bottom: 20px; border: 1px solid var(--border); border-radius: 4px;">
                    <option value="">-- Choose Recipient --</option>
                    <option value="acme">Acme Corp (US)</option>
                    <option value="global">Global Supplies Ltd (UK)</option>
                </select>
                <p class="error-text" id="recipient-error" data-testid="recipient-error">Please select a recipient.</p>

                <p style="font-weight: 600; margin-bottom: 5px;">Account Number (22 digits):</p>
                <input type="text" id="acc-number" data-testid="acc-number" maxlength="22" placeholder="1234567890123456789012" style="width: 100%; padding: 12px; margin-bottom: 20px; border: 1px solid var(--border); border-radius: 4px; box-sizing: border-box;">
                <p class="error-text" id="acc-error" data-testid="acc-error">Account must be exactly 22 digits.</p>

                <button data-testid="btn-next-1" onclick="validateStep1()" style="background: var(--primary); color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; font-weight: bold;">Next Step</button>
            </div>

            <!-- STEP 2 -->
            <div id="step2-form" data-testid="step-2-form" style="display: none;">
                <p style="font-weight: 600; margin-bottom: 5px;">Transfer Date:</p>
                <input type="date" id="transfer-date" data-testid="transfer-date" style="width: 100%; padding: 12px; margin-bottom: 20px; border: 1px solid var(--border); border-radius: 4px; box-sizing: border-box;">
                <p class="error-text" id="date-error" data-testid="date-error">Date cannot be in the past.</p>

                <p style="font-weight: 600; margin-bottom: 5px;">Enter amount (USD):</p>
                <input type="number" id="transfer-amount" data-testid="transfer-amount" placeholder="$ 0.00" style="width: 100%; padding: 12px; margin-bottom: 20px; border: 1px solid var(--border); border-radius: 4px; box-sizing: border-box;">
                <p class="error-text" id="amount-error" data-testid="amount-error">Amount must be greater than $0.</p>

                <button data-testid="btn-back-2" onclick="goToStep(1)" style="background: var(--surface-color); color: var(--text-main); border: 1px solid var(--border); padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-right: 10px; font-weight: bold;">Back</button>
                <button data-testid="btn-next-2" onclick="validateStep2()" style="background: var(--primary); color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; font-weight: bold;">Review Transfer</button>
            </div>

            <!-- STEP 3 -->
            <div id="step3-form" data-testid="step-3-form" style="display: none;">
                <div style="background: var(--bg-color); padding: 20px; border: 1px dashed var(--border); border-radius: 8px; margin-bottom: 20px;">
                    <p style="margin-top: 0;"><strong>To:</strong> <span id="review-recipient" data-testid="review-recipient"></span></p>
                    <p><strong>Account:</strong> <span id="review-acc" data-testid="review-acc"></span></p>
                    <p style="margin-bottom: 0;"><strong>Amount:</strong> $<span id="review-amount" data-testid="review-amount"></span></p>
                </div>
                <button data-testid="btn-back-3" onclick="goToStep(2)" style="background: var(--surface-color); color: var(--text-main); border: 1px solid var(--border); padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-right: 10px; font-weight: bold;">Back</button>
                <button id="btn-submit-transfer" data-testid="btn-submit-transfer" style="background: var(--success); color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; font-weight: bold;" onclick="submitTransfer()">Confirm & Send</button>
            </div>
            
            <div id="success-msg" data-testid="success-msg" style="display: none; color: var(--success); font-weight: bold; margin-top: 20px; padding: 15px; background: rgba(40, 167, 69, 0.1); border-radius: 4px; border-left: 4px solid var(--success);">
                ✅ Transfer Submitted Successfully!
            </div>
        </div>
    </div>

    <script>
        function goToStep(step) {
            document.querySelectorAll('.step').forEach(el => el.classList.remove('active'));
            document.getElementById('step1-form').style.display = 'none';
            document.getElementById('step2-form').style.display = 'none';
            document.getElementById('step3-form').style.display = 'none';
            document.getElementById(`step${step}-indicator`).classList.add('active');
            document.getElementById(`step${step}-form`).style.display = 'block';
        }

        function validateStep1() {
            let valid = true;
            if(!document.getElementById('recipient-select').value) { document.getElementById('recipient-error').style.display = 'block'; valid = false; } else { document.getElementById('recipient-error').style.display = 'none'; }
            if(document.getElementById('acc-number').value.length !== 10) { document.getElementById('acc-error').style.display = 'block'; valid = false; } else { document.getElementById('acc-error').style.display = 'none'; }
            if(valid) goToStep(2);
        }

        function validateStep2() {
            let valid = true;
            const amt = parseFloat(document.getElementById('transfer-amount').value);
            const date = document.getElementById('transfer-date').value;
            const today = new Date().toISOString().split('T')[0];
            
            if(!amt || amt <= 0) { document.getElementById('amount-error').style.display = 'block'; valid = false; } else { document.getElementById('amount-error').style.display = 'none'; }
            if(!date || date < today) { document.getElementById('date-error').style.display = 'block'; valid = false; } else { document.getElementById('date-error').style.display = 'none'; }
            
            if(valid) {
                document.getElementById('review-recipient').innerText = document.getElementById('recipient-select').options[document.getElementById('recipient-select').selectedIndex].text;
                document.getElementById('review-acc').innerText = document.getElementById('acc-number').value;
                document.getElementById('review-amount').innerText = amt.toFixed(2);
                goToStep(3);
            }
        }

        function submitTransfer() {
            document.getElementById('btn-submit-transfer').disabled = true;
            document.getElementById('success-msg').style.display = 'block';
        }
    </script>
</body>
</html>
--- settings.html
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <meta charset="UTF-8">
    <title>Settings - WorldBank Mock</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <!-- UNIVERSAL NAVIGATION HEADER -->
    <header class="universal-header">
        <div class="header-top">
            <div class="brand-section">
                <a href="index.html" class="logo-text">WorldBank QA</a>
                <div class="pill-toggle">
                    <a href="index.html" data-testid="toggle-public">Public Sandbox</a>
                    <a href="dashboard.html" class="active" data-testid="toggle-secure">Secure Sandbox</a>
                </div>
            </div>
            <div class="header-utilities">
                <a href="#">About</a>
                <a href="#">Support 24/7</a>
                <a href="#">Contacts</a>
                <a href="#">EN 🌐</a>
            </div>
        </div>
        <div class="header-bottom">
            <div class="main-nav-links">
                <a href="index.html" data-testid="nav-home">Home</a>
                <a href="dashboard.html" data-testid="nav-dashboard">Dashboard</a>
                <a href="transfer.html" data-testid="nav-transfer">Wire Transfer</a>
                <a href="settings.html" data-testid="nav-settings" style="color: var(--nav-active);">Settings</a>
            </div>
            <div>
                <a href="login.html" class="action-btn" data-testid="nav-login-btn">Secure Login / Logout</a>
            </div>
        </div>
    </header>

    <div class="container">
        <!-- PAGE-LEVEL CONTROL BAR -->
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
            <h2 data-testid="app-title" style="margin: 0; color: var(--primary);">User Preferences</h2>
            <button onclick="window.location.href='dashboard.html'" style="padding: 8px 16px; cursor: pointer; border-radius: 4px; border: 1px solid var(--border); background: var(--surface-color); color: var(--text-main);">Back to Dashboard</button>
        </div>

        <div class="card">
            <p style="color: var(--text-muted); font-size: 1.1rem; margin-top: 0;">Welcome to the settings panel. Configuration options are managed by your corporate administrator.</p>
            
            <hr style="margin: 20px 0; border: 0; border-top: 1px solid var(--border);">
            
            <label style="display: flex; align-items: center; margin-bottom: 15px; font-size: 1.05rem; cursor: pointer;">
                <input type="checkbox" checked data-testid="chk-email-alerts" style="width: 18px; height: 18px; margin-right: 10px;"> 
                Enable Email Alerts
            </label>
            <label style="display: flex; align-items: center; margin-bottom: 25px; font-size: 1.05rem; cursor: pointer;">
                <input type="checkbox" data-testid="chk-sms-alerts" style="width: 18px; height: 18px; margin-right: 10px;"> 
                Enable SMS Alerts
            </label>
            
            <button data-testid="btn-save-settings" onclick="alert('Settings Saved Successfully!')" style="background: var(--primary); color: white; border: none; padding: 12px 24px; border-radius: 4px; font-size: 1rem; font-weight: bold; cursor: pointer; transition: background 0.3s;">
                Save Changes
            </button>
        </div>
    </div>
</body>
</html>
--- styles.css
/* =========================================
   1. VARIABLES & THEMING
   ========================================= */
:root {
  --bg-color: #f4f7f6;
  --surface-color: #ffffff;
  --text-main: #333333;
  --text-muted: #777777;
  --primary: #005A9C;
  --border: #dddddd;
  --success: #28a745;
  --warning: #ffc107;
  --danger: #dc3545;
  --nav-active: #e3000f; /* Bold Red */
  --nav-hover: #b3000c;
}

/* Dark Mode Overrides */
[data-theme="dark"] {
  --bg-color: #121212;
  --surface-color: #1e1e1e;
  --text-main: #e0e0e0;
  --text-muted: #aaaaaa;
  --primary: #4da3ff;
  --border: #333333;
  --nav-active: #ff4d58; /* Lighter red for dark mode contrast */
  --nav-hover: #ff7a83;
}

/* =========================================
   2. BASE STYLES
   ========================================= */
body {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background: var(--bg-color);
  color: var(--text-main);
  margin: 0; /* Ensures the header touches the edges */
  transition: background 0.3s, color 0.3s;
}

/* =========================================
   3. UNIVERSAL NAVIGATION
   ========================================= */
.universal-header {
  background-color: var(--surface-color);
  border-bottom: 1px solid var(--border);
  width: 100%;
  position: sticky;
  top: 0;
  z-index: 100;
  transition: background-color 0.3s;
}

/* Top Tier */
.header-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 40px;
  border-bottom: 1px solid var(--border);
}

.brand-section {
  display: flex;
  align-items: center;
  gap: 30px;
}

.logo-text {
  font-size: 1.6rem;
  font-weight: 800;
  color: var(--nav-active);
  text-decoration: none;
  letter-spacing: -0.5px;
}

/* Environment Toggle Pills */
.pill-toggle {
  display: flex;
  background: var(--bg-color);
  border-radius: 30px;
  overflow: hidden;
  border: 1px solid var(--border);
}

.pill-toggle a {
  padding: 8px 20px;
  text-decoration: none;
  color: var(--text-main);
  font-weight: 600;
  font-size: 0.9rem;
  transition: all 0.3s ease;
}

.pill-toggle a.active {
  background: var(--nav-active);
  color: white;
  border-radius: 30px;
}

/* Utilities (Top Right) */
.header-utilities a {
  color: var(--text-main);
  text-decoration: none;
  margin-left: 20px;
  font-size: 0.9rem;
  font-weight: 600;
  transition: color 0.2s;
}

.header-utilities a:hover {
  color: var(--nav-active);
}

/* Bottom Tier (Main Links) */
.header-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 40px;
}

.main-nav-links {
  display: flex;
  gap: 30px;
}

.main-nav-links a {
  color: var(--text-main);
  text-decoration: none;
  font-weight: 600;
  font-size: 1.05rem;
  transition: color 0.2s;
}

.main-nav-links a:hover {
  color: var(--nav-active);
}

/* Login/Logout Button */
.action-btn {
  background: var(--nav-active);
  color: white;
  padding: 10px 28px;
  border-radius: 30px;
  text-decoration: none;
  font-weight: bold;
  font-size: 1rem;
  transition: background 0.3s, transform 0.1s;
  display: inline-block;
}

.action-btn:hover {
  background: var(--nav-hover);
  transform: scale(1.02);
}

/* =========================================
   4. LAYOUT & CONTAINERS
   ========================================= */
.container {
  max-width: 1200px;
  margin: 2rem auto;
  padding: 0 1rem;
  display: grid;
  gap: 2rem;
}

.card {
  background: var(--surface-color);
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  border: 1px solid var(--border);
  transition: background-color 0.3s, border-color 0.3s;
}

/* =========================================
   5. UI COMPONENTS
   ========================================= */
/* Badges */
.badge { 
  padding: 4px 8px; 
  border-radius: 12px; 
  font-size: 0.8rem; 
  font-weight: bold; 
  color: white; 
}
.bg-success { background: var(--success); }
.bg-warning { background: var(--warning); color: #333; }
.bg-danger { background: var(--danger); }

/* Ledger Table */
table { width: 100%; border-collapse: collapse; }
th, td { padding: 1rem; text-align: left; border-bottom: 1px solid var(--border); }
th { cursor: pointer; }
th:hover { background: rgba(0,0,0,0.05); }
[data-theme="dark"] th:hover { background: rgba(255,255,255,0.05); }

/* Overlays & Modals */
.overlay {
  display: none; position: fixed; top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.5); z-index: 999; backdrop-filter: blur(2px);
}
.overlay.active { display: block; }

.modal {
  position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
  background: var(--surface-color); padding: 2rem; border-radius: 8px;
  z-index: 1000; display: none; min-width: 300px;
}
.modal.active { display: block; }

/* Stepper UI */
.stepper {
  display: flex; justify-content: space-between; margin-bottom: 2rem; position: relative;
}
.stepper::before {
  content: ''; position: absolute; top: 15px; left: 0; right: 0; height: 2px; background: var(--border); z-index: 1;
}
.step {
  position: relative; z-index: 2; background: var(--surface-color); padding: 0 10px;
  display: flex; flex-direction: column; align-items: center; color: var(--text-muted);
}
.step .circle {
  width: 30px; height: 30px; border-radius: 50%; background: var(--border);
  display: flex; justify-content: center; align-items: center; font-weight: bold; margin-bottom: 5px;
}
.step.active { color: var(--primary); font-weight: bold; }
.step.active .circle { background: var(--primary); color: white; }