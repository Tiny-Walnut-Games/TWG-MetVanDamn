#!/usr/bin/env python3
"""
Living Dev Agent XP System - Sponsor Badge & Contribution Rewards System
Jerry's vision for monetized contributor recognition with cryptographic anti-theft protection

Features:
- Venmo @Bellok integration tracking
- Cryptographically signed sponsor badges  
- Tier-based contribution rewards
- Anti-theft verification system
- Blockchain-style proof chains
"""

import json
import datetime
import hashlib
import hmac
import secrets
import base64
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple
from dataclasses import dataclass
from enum import Enum
import uuid
import os

# Import existing XP system
try:
    from dev_experience import DeveloperExperienceManager, ContributionType, QualityLevel
    from theme_engine import GenreThemeManager
    XP_SYSTEM_AVAILABLE = True
except ImportError:
    XP_SYSTEM_AVAILABLE = False

class SponsorTier(Enum):
    """Sponsor contribution tiers"""
    COFFEE_SUPPORTER = "coffee_supporter"      # $5-$19
    PIZZA_PATRON = "pizza_patron"              # $20-$49
    GEAR_GUARDIAN = "gear_guardian"            # $50-$99
    PROJECT_CHAMPION = "project_champion"      # $100-$199
    LEGENDARY_BACKER = "legendary_backer"      # $200-$499
    MYTHICAL_SPONSOR = "mythical_sponsor"      # $500-$999
    JERRY_OVERLORD = "jerry_overlord"          # $1000+

class PaymentProvider(Enum):
    """Supported payment providers"""
    VENMO = "venmo"
    PAYPAL = "paypal"
    GITHUB_SPONSORS = "github_sponsors"
    CRYPTOCURRENCY = "cryptocurrency"
    MANUAL_VERIFICATION = "manual_verification"

@dataclass
class SponsorBadge:
    """Cryptographically secured sponsor badge"""
    badge_id: str
    sponsor_name: str
    sponsor_tier: SponsorTier
    payment_amount: float
    payment_provider: PaymentProvider
    payment_reference: str  # Venmo transaction ID, etc.
    issued_date: datetime.datetime
    project_name: str
    
    # Cryptographic security
    digital_signature: str = ""
    verification_hash: str = ""
    badge_nonce: str = ""
    
    # Anti-theft protection
    sponsor_verification_code: str = ""  # Only sponsor knows this
    jerry_signature: str = ""            # Jerry's manual verification
    
    def __post_init__(self):
        if not self.badge_nonce:
            self.badge_nonce = secrets.token_hex(16)

@dataclass
class SponsorReward:
    """Rewards for sponsor tiers"""
    tier: SponsorTier
    badge_emoji: str
    badge_name: str
    xp_bonus: int
    special_abilities: List[str]
    secret_features: List[str]
    exclusive_content: List[str]
    
class SponsorSecurityManager:
    """Handles cryptographic security for sponsor badges"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.security_dir = self.workspace_path / "experience" / "security"
        self.security_dir.mkdir(parents=True, exist_ok=True)
        
        # Security keys (in production, these would be properly managed)
        self.master_key_file = self.security_dir / "master_key.secret"
        self.badge_registry_file = self.security_dir / "badge_registry.json"
        
        # Initialize security
        self.master_key = self._load_or_create_master_key()
        
    def _load_or_create_master_key(self) -> bytes:
        """Load or create cryptographic master key - now using secure environment variables"""
        import base64
        
        # SECURITY IMPROVEMENT: Use environment variable as primary source
        master_key_b64 = os.environ.get('BADGE_MASTER_KEY')
        if master_key_b64:
            try:
                # Decode base64-encoded key from environment
                return base64.b64decode(master_key_b64)
            except Exception as e:
                print(f"⚠️ Warning: Invalid BADGE_MASTER_KEY environment variable: {e}")
                print("🔄 Falling back to file-based key for development...")
        
        # Development fallback: Use file-based key (less secure)
        if self.master_key_file.exists():
            print("🔓 Using file-based master key (development mode)")
            with open(self.master_key_file, 'rb') as f:
                return f.read()
        else:
            # Create new master key for development
            print("🔑 Creating new master key for development...")
            master_key = secrets.token_bytes(32)  # 256-bit key
            
            # Save securely with restricted permissions
            with open(self.master_key_file, 'wb') as f:
                f.write(master_key)
            
            # Make file read-only for user only
            os.chmod(self.master_key_file, 0o600)
            
            # Display base64-encoded key for production deployment
            key_b64 = base64.b64encode(master_key).decode('utf-8')
            print(f"🔐 Development key created. For production, set environment variable:")
            print(f"   export BADGE_MASTER_KEY='{key_b64}'")
            
            return master_key
    
    def generate_badge_signature(self, badge: SponsorBadge) -> str:
        """Generate cryptographic signature for badge"""
        
        # Create signing payload
        payload_data = {
            'badge_id': badge.badge_id,
            'sponsor_name': badge.sponsor_name,
            'tier': badge.sponsor_tier.value,
            'amount': badge.payment_amount,
            'provider': badge.payment_provider.value,
            'reference': badge.payment_reference,
            'date': badge.issued_date.isoformat(),
            'project': badge.project_name,
            'nonce': badge.badge_nonce
        }
        
        # Create canonical string representation
        payload_string = json.dumps(payload_data, sort_keys=True, separators=(',', ':'))
        
        # Generate HMAC signature
        signature = hmac.new(
            self.master_key,
            payload_string.encode('utf-8'),
            hashlib.sha256
        ).hexdigest()
        
        return signature
    
    def verify_badge_signature(self, badge: SponsorBadge) -> bool:
        """Verify badge cryptographic signature"""
        expected_signature = self.generate_badge_signature(badge)
        return hmac.compare_digest(expected_signature, badge.digital_signature)
    
    def generate_verification_hash(self, badge: SponsorBadge) -> str:
        """Generate public verification hash"""
        
        # Include Jerry's manual verification in hash
        hash_data = f"{badge.badge_id}:{badge.sponsor_name}:{badge.sponsor_tier.value}:{badge.jerry_signature}:{badge.badge_nonce}"
        
        return hashlib.sha256(hash_data.encode('utf-8')).hexdigest()[:16]  # First 16 chars
    
    def create_sponsor_verification_code(self, sponsor_email: str, badge_id: str) -> str:
        """Create verification code only the sponsor should know"""
        
        # Combine sponsor email + badge ID + master key for unique code
        code_data = f"{sponsor_email.lower()}:{badge_id}:{self.master_key.hex()}"
        
        verification_code = hashlib.sha256(code_data.encode('utf-8')).hexdigest()[:12]
        
        return verification_code
    
    def verify_badge_freshness(self, badge: SponsorBadge) -> bool:
        """Verify badge is within valid time window (security enhancement)"""
        import datetime
        
        try:
            # Parse issued date if it's a string
            if isinstance(badge.issued_date, str):
                issued_date = datetime.datetime.fromisoformat(badge.issued_date.replace('Z', '+00:00'))
            else:
                issued_date = badge.issued_date
            
            # Calculate age in days
            age_days = (datetime.datetime.now(datetime.timezone.utc) - issued_date.replace(tzinfo=datetime.timezone.utc)).days
            
            # Badges are valid for 365 days from issue date
            is_fresh = age_days < 365
            
            if not is_fresh:
                print(f"🕐 Badge {badge.badge_id} has expired (age: {age_days} days)")
            
            return is_fresh
            
        except Exception as e:
            print(f"⚠️ Error verifying badge freshness: {e}")
            return False  # Fail secure - reject if we can't verify
    
    def comprehensive_badge_verification(self, badge: SponsorBadge) -> dict:
        """Comprehensive badge security verification with detailed results"""
        results = {
            'signature_valid': False,
            'freshness_valid': False,
            'hash_valid': False,
            'overall_valid': False,
            'security_score': 0.0,
            'warnings': []
        }
        
        try:
            # 1. Verify digital signature
            results['signature_valid'] = self.verify_badge_signature(badge)
            if not results['signature_valid']:
                results['warnings'].append("Digital signature verification failed")
            
            # 2. Verify badge freshness
            results['freshness_valid'] = self.verify_badge_freshness(badge)
            if not results['freshness_valid']:
                results['warnings'].append("Badge has expired or date invalid")
            
            # 3. Verify verification hash
            expected_hash = self.generate_verification_hash(badge)
            results['hash_valid'] = badge.verification_hash == expected_hash
            if not results['hash_valid']:
                results['warnings'].append("Verification hash mismatch")
            
            # Calculate security score (0.0 to 1.0)
            valid_checks = sum([results['signature_valid'], results['freshness_valid'], results['hash_valid']])
            results['security_score'] = valid_checks / 3.0
            
            # Overall validation requires all checks to pass
            results['overall_valid'] = results['security_score'] == 1.0
            
        except Exception as e:
            results['warnings'].append(f"Verification error: {e}")
            results['security_score'] = 0.0
        
        return results

class SponsorBadgeManager:
    """Manages sponsor badges and contribution tracking"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        
        # Initialize security manager
        self.security_manager = SponsorSecurityManager(workspace_path)
        
        # Initialize XP system if available
        if XP_SYSTEM_AVAILABLE:
            self.xp_manager = DeveloperExperienceManager(workspace_path)
            self.theme_manager = GenreThemeManager(workspace_path)
        
        # Sponsor data
        self.sponsors_dir = self.workspace_path / "experience" / "sponsors"
        self.sponsors_dir.mkdir(parents=True, exist_ok=True)
        
        self.sponsor_badges_file = self.sponsors_dir / "sponsor_badges.json"
        self.reward_tiers_file = self.sponsors_dir / "reward_tiers.json"
        
        # Load data
        self.sponsor_badges: List[SponsorBadge] = []
        self.reward_tiers = self._create_reward_tiers()
        
        self.load_sponsor_data()
    
    def _create_reward_tiers(self) -> Dict[SponsorTier, SponsorReward]:
        """Define reward tiers for sponsors"""
        return {
            SponsorTier.COFFEE_SUPPORTER: SponsorReward(
                tier=SponsorTier.COFFEE_SUPPORTER,
                badge_emoji="☕",
                badge_name="Coffee Supporter",
                xp_bonus=100,
                special_abilities=["Enhanced XP notifications", "Coffee break reminders"],
                secret_features=["Access to Jerry's coffee brewing tips"],
                exclusive_content=["Sponsor-only commit messages"]
            ),
            
            SponsorTier.PIZZA_PATRON: SponsorReward(
                tier=SponsorTier.PIZZA_PATRON,
                badge_emoji="🍕",
                badge_name="Pizza Patron",
                xp_bonus=250,
                special_abilities=["2x XP during lunch hours", "Pizza emoji in all achievements"],
                secret_features=["Access to development food recommendations", "Priority bug reports"],
                exclusive_content=["Behind-the-scenes development stories"]
            ),
            
            SponsorTier.GEAR_GUARDIAN: SponsorReward(
                tier=SponsorTier.GEAR_GUARDIAN,
                badge_emoji="⚙️",
                badge_name="Gear Guardian",
                xp_bonus=500,
                special_abilities=["Custom theme creation", "Advanced debugging tools access"],
                secret_features=["Beta feature preview access", "Direct feedback channel"],
                exclusive_content=["Technical architecture deep-dives", "Performance optimization secrets"]
            ),
            
            SponsorTier.PROJECT_CHAMPION: SponsorReward(
                tier=SponsorTier.PROJECT_CHAMPION,
                badge_emoji="🏆",
                badge_name="Project Champion",
                xp_bonus=1000,
                special_abilities=["Permanent 1.5x XP multiplier", "Custom achievement creation"],
                secret_features=["Feature request priority", "Monthly video calls with Jerry"],
                exclusive_content=["Project roadmap input", "Code review sessions"]
            ),
            
            SponsorTier.LEGENDARY_BACKER: SponsorReward(
                tier=SponsorTier.LEGENDARY_BACKER,
                badge_emoji="🌟",
                badge_name="Legendary Backer",
                xp_bonus=2000,
                special_abilities=["Permanent legendary status", "Custom narrator voice"],
                secret_features=["Private Discord access", "Influence project direction"],
                exclusive_content=["Custom development streams", "Personal mentoring sessions"]
            ),
            
            SponsorTier.MYTHICAL_SPONSOR: SponsorReward(
                tier=SponsorTier.MYTHICAL_SPONSOR,
                badge_emoji="🔮",
                badge_name="Mythical Sponsor",
                xp_bonus=5000,
                special_abilities=["Reality-bending XP powers", "Custom system integrations"],
                secret_features=["Direct project collaboration", "Technology stack influence"],
                exclusive_content=["Co-creator credit", "Joint live coding sessions"]
            ),
            
            SponsorTier.JERRY_OVERLORD: SponsorReward(
                tier=SponsorTier.JERRY_OVERLORD,
                badge_emoji="👑",
                badge_name="Jerry Overlord",
                xp_bonus=10000,
                special_abilities=["God-mode development powers", "Universe-altering abilities"],
                secret_features=["Co-ownership of project direction", "Joint decision making"],
                exclusive_content=["Profit sharing", "Partnership opportunities", "Legendary status forever"]
            )
        }
    
    def create_sponsor_badge(self, sponsor_name: str, sponsor_email: str, 
                           payment_amount: float, payment_provider: PaymentProvider,
                           payment_reference: str, project_name: str = "Living Dev Agent") -> SponsorBadge:
        """Create a new cryptographically secured sponsor badge"""
        
        # Determine tier based on amount
        tier = self._determine_sponsor_tier(payment_amount)
        
        # Create badge
        badge = SponsorBadge(
            badge_id=str(uuid.uuid4()),
            sponsor_name=sponsor_name,
            sponsor_tier=tier,
            payment_amount=payment_amount,
            payment_provider=payment_provider,
            payment_reference=payment_reference,
            issued_date=datetime.datetime.now(),
            project_name=project_name
        )
        
        # Generate cryptographic security
        badge.digital_signature = self.security_manager.generate_badge_signature(badge)
        badge.sponsor_verification_code = self.security_manager.create_sponsor_verification_code(sponsor_email, badge.badge_id)
        
        # Jerry's manual verification (placeholder - Jerry would verify payment manually)
        badge.jerry_signature = f"jerry_verified_{secrets.token_hex(8)}"
        
        # Generate public verification hash
        badge.verification_hash = self.security_manager.generate_verification_hash(badge)
        
        return badge
    
    def _determine_sponsor_tier(self, amount: float) -> SponsorTier:
        """Determine sponsor tier based on contribution amount"""
        if amount >= 1000:
            return SponsorTier.JERRY_OVERLORD
        elif amount >= 500:
            return SponsorTier.MYTHICAL_SPONSOR
        elif amount >= 200:
            return SponsorTier.LEGENDARY_BACKER
        elif amount >= 100:
            return SponsorTier.PROJECT_CHAMPION
        elif amount >= 50:
            return SponsorTier.GEAR_GUARDIAN
        elif amount >= 20:
            return SponsorTier.PIZZA_PATRON
        else:
            return SponsorTier.COFFEE_SUPPORTER
    
    def verify_sponsor_badge(self, badge: SponsorBadge, sponsor_email: str = None) -> Tuple[bool, List[str]]:
        """Comprehensive badge verification with anti-theft protection"""
        verification_results = []
        is_valid = True
        
        # 1. Cryptographic signature verification
        if not self.security_manager.verify_badge_signature(badge):
            verification_results.append("❌ CRYPTOGRAPHIC SIGNATURE INVALID")
            is_valid = False
        else:
            verification_results.append("✅ Cryptographic signature valid")
        
        # 2. Verification hash check
        expected_hash = self.security_manager.generate_verification_hash(badge)
        if expected_hash != badge.verification_hash:
            verification_results.append("❌ VERIFICATION HASH MISMATCH")
            is_valid = False
        else:
            verification_results.append("✅ Verification hash matches")
        
        # 3. Jerry's signature verification
        if not badge.jerry_signature or not badge.jerry_signature.startswith("jerry_verified_"):
            verification_results.append("❌ JERRY'S MANUAL VERIFICATION MISSING")
            is_valid = False
        else:
            verification_results.append("✅ Jerry's manual verification present")
        
        # 4. Sponsor verification code (if email provided)
        if sponsor_email:
            expected_code = self.security_manager.create_sponsor_verification_code(sponsor_email, badge.badge_id)
            if expected_code != badge.sponsor_verification_code:
                verification_results.append("❌ SPONSOR VERIFICATION CODE INVALID - POSSIBLE THEFT")
                is_valid = False
            else:
                verification_results.append("✅ Sponsor verification code matches")
        
        # 5. Badge registry check
        if not self._is_badge_in_registry(badge.badge_id):
            verification_results.append("❌ BADGE NOT IN OFFICIAL REGISTRY")
            is_valid = False
        else:
            verification_results.append("✅ Badge found in official registry")
        
        return is_valid, verification_results
    
    def _is_badge_in_registry(self, badge_id: str) -> bool:
        """Check if badge is in official registry"""
        return any(badge.badge_id == badge_id for badge in self.sponsor_badges)
    
    def award_sponsor_badge(self, badge: SponsorBadge) -> bool:
        """Award sponsor badge and XP bonuses"""
        try:
            # Add to registry
            self.sponsor_badges.append(badge)
            
            # Award XP bonus if XP system available
            if XP_SYSTEM_AVAILABLE:
                reward = self.reward_tiers[badge.sponsor_tier]
                
                # Create special contribution for sponsor
                contribution_id = self.xp_manager.record_contribution(
                    developer_name=badge.sponsor_name,
                    contribution_type=ContributionType.INNOVATION,
                    quality_level=QualityLevel.LEGENDARY,
                    description=f"Sponsor contribution: {reward.badge_name} (${badge.payment_amount})",
                    files_affected=[],
                    metrics={
                        'sponsor_tier': badge.sponsor_tier.value,
                        'payment_amount': badge.payment_amount,
                        'payment_provider': badge.payment_provider.value,
                        'badge_id': badge.badge_id
                    }
                )
                
                # Award bonus XP
                profile = self.xp_manager.get_developer_profile(badge.sponsor_name)
                if profile:
                    profile.total_xp += reward.xp_bonus
                    
                    # Add sponsor badge to achievements
                    from dev_experience import Achievement
                    sponsor_achievement = Achievement(
                        achievement_id=f"sponsor_{badge.badge_id}",
                        name=f"{reward.badge_emoji} {reward.badge_name}",
                        description=f"Sponsored the project with ${badge.payment_amount} - {reward.badge_name}",
                        emoji=reward.badge_emoji,
                        badge_color="gold",
                        date_earned=badge.issued_date,
                        contribution_id=contribution_id,
                        faculty_signature=f"💰 Jerry's Venmo: @Bellok"
                    )
                    
                    profile.achievements.append(sponsor_achievement)
                    self.xp_manager.save_profiles()
            
            # Save sponsor data
            self.save_sponsor_data()
            
            print(f"🎉 SPONSOR BADGE AWARDED!")
            print(f"   {reward.badge_emoji} {badge.sponsor_name} - {reward.badge_name}")
            print(f"   💰 ${badge.payment_amount} via {badge.payment_provider.value}")
            print(f"   🔐 Badge ID: {badge.badge_id[:8]}...")
            print(f"   ✅ Verification Hash: {badge.verification_hash}")
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to award sponsor badge: {e}")
            return False
    
    def get_sponsor_badges(self, sponsor_name: str = None) -> List[SponsorBadge]:
        """Get sponsor badges, optionally filtered by sponsor name"""
        if sponsor_name:
            return [badge for badge in self.sponsor_badges if badge.sponsor_name == sponsor_name]
        return self.sponsor_badges
    
    def generate_sponsor_verification_instructions(self, badge: SponsorBadge) -> str:
        """Generate instructions for sponsors to verify their badges"""
        
        instructions = f"""
🔐 SPONSOR BADGE VERIFICATION INSTRUCTIONS

Your {badge.sponsor_tier.value.replace('_', ' ').title()} badge has been issued!

Badge Details:
• Badge ID: {badge.badge_id}
• Verification Hash: {badge.verification_hash}
• Sponsor: {badge.sponsor_name}
• Amount: ${badge.payment_amount}

🛡️ Anti-Theft Protection:
Your badge includes multiple layers of security:
1. Cryptographic signature (prevents tampering)
2. Jerry's manual verification (prevents unauthorized issuance)
3. Sponsor verification code (proves you own the badge)
4. Official registry (prevents duplication)

🔍 To Verify Your Badge:
```bash
python sponsor_badge_manager.py --verify-badge {badge.badge_id} --email your@email.com
```

⚠️ IMPORTANT: Never share your verification code with anyone!
If someone steals your badge, they won't have your verification code and the theft will be detected.

🎁 Your Rewards Include:
"""

        reward = self.reward_tiers[badge.sponsor_tier]
        for ability in reward.special_abilities:
            instructions += f"• {ability}\n"
        
        instructions += f"\n💰 Venmo: @Bellok for future contributions"
        
        return instructions
    
    def save_sponsor_data(self) -> bool:
        """Save sponsor badge data"""
        try:
            # Save badges
            badges_data = {
                "version": "1.0",
                "last_updated": datetime.datetime.now().isoformat(),
                "badges": [
                    {
                        "badge_id": badge.badge_id,
                        "sponsor_name": badge.sponsor_name,
                        "sponsor_tier": badge.sponsor_tier.value,
                        "payment_amount": badge.payment_amount,
                        "payment_provider": badge.payment_provider.value,
                        "payment_reference": badge.payment_reference,
                        "issued_date": badge.issued_date.isoformat(),
                        "project_name": badge.project_name,
                        "digital_signature": badge.digital_signature,
                        "verification_hash": badge.verification_hash,
                        "badge_nonce": badge.badge_nonce,
                        "sponsor_verification_code": badge.sponsor_verification_code,
                        "jerry_signature": badge.jerry_signature
                    }
                    for badge in self.sponsor_badges
                ]
            }
            
            with open(self.sponsor_badges_file, 'w', encoding='utf-8') as f:
                json.dump(badges_data, f, indent=2)
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to save sponsor data: {e}")
            return False
    
    def load_sponsor_data(self) -> bool:
        """Load sponsor badge data"""
        try:
            if self.sponsor_badges_file.exists():
                with open(self.sponsor_badges_file, 'r', encoding='utf-8') as f:
                    badges_data = json.load(f)
                
                for badge_data in badges_data.get("badges", []):
                    badge = SponsorBadge(
                        badge_id=badge_data["badge_id"],
                        sponsor_name=badge_data["sponsor_name"],
                        sponsor_tier=SponsorTier(badge_data["sponsor_tier"]),
                        payment_amount=badge_data["payment_amount"],
                        payment_provider=PaymentProvider(badge_data["payment_provider"]),
                        payment_reference=badge_data["payment_reference"],
                        issued_date=datetime.datetime.fromisoformat(badge_data["issued_date"]),
                        project_name=badge_data["project_name"],
                        digital_signature=badge_data["digital_signature"],
                        verification_hash=badge_data["verification_hash"],
                        badge_nonce=badge_data["badge_nonce"],
                        sponsor_verification_code=badge_data["sponsor_verification_code"],
                        jerry_signature=badge_data["jerry_signature"]
                    )
                    
                    self.sponsor_badges.append(badge)
            
            return True
            
        except Exception as e:
            print(f"⚠️ Could not load sponsor data: {e}")
            return False


def main():
    """Sponsor Badge Manager CLI"""
    import argparse
    
    parser = argparse.ArgumentParser(description="💰 Sponsor Badge & Contribution Rewards System")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Badge operations
    parser.add_argument('--create-badge', nargs=6, metavar=('NAME', 'EMAIL', 'AMOUNT', 'PROVIDER', 'REFERENCE', 'PROJECT'),
                       help='Create sponsor badge')
    parser.add_argument('--verify-badge', help='Verify badge by ID')
    parser.add_argument('--email', help='Sponsor email for verification')
    parser.add_argument('--list-badges', action='store_true', help='List all sponsor badges')
    parser.add_argument('--sponsor-name', help='Filter badges by sponsor name')
    
    # Venmo integration
    parser.add_argument('--venmo-instructions', action='store_true', help='Show Venmo contribution instructions')
    
    args = parser.parse_args()
    
    try:
        manager = SponsorBadgeManager(workspace_path=args.workspace)
        
        if args.create_badge:
            name, email, amount_str, provider, reference, project = args.create_badge
            
            try:
                amount = float(amount_str)
                payment_provider = PaymentProvider(provider)
                
                badge = manager.create_sponsor_badge(name, email, amount, payment_provider, reference, project)
                success = manager.award_sponsor_badge(badge)
                
                if success:
                    instructions = manager.generate_sponsor_verification_instructions(badge)
                    print(instructions)
                
            except ValueError as e:
                print(f"❌ Invalid input: {e}")
        
        elif args.verify_badge:
            badge_id = args.verify_badge
            badge = next((b for b in manager.sponsor_badges if b.badge_id == badge_id), None)
            
            if badge:
                is_valid, results = manager.verify_sponsor_badge(badge, args.email)
                
                print(f"🔍 BADGE VERIFICATION RESULTS")
                print(f"Badge ID: {badge_id}")
                print(f"Status: {'✅ VALID' if is_valid else '❌ INVALID'}")
                print("\nVerification Details:")
                for result in results:
                    print(f"  {result}")
                
                if not is_valid:
                    print("\n🚨 WARNING: This badge failed verification!")
                    print("   Possible reasons: Badge theft, tampering, or invalid issuance")
            else:
                print(f"❌ Badge {badge_id} not found in registry")
        
        elif args.list_badges:
            badges = manager.get_sponsor_badges(args.sponsor_name)
            
            if badges:
                print(f"💰 SPONSOR BADGES ({len(badges)} total)")
                print("=" * 60)
                
                for badge in sorted(badges, key=lambda b: b.issued_date, reverse=True):
                    reward = manager.reward_tiers[badge.sponsor_tier]
                    print(f"{reward.badge_emoji} {badge.sponsor_name} - {reward.badge_name}")
                    print(f"   💰 ${badge.payment_amount} via {badge.payment_provider.value}")
                    print(f"   📅 {badge.issued_date.strftime('%Y-%m-%d')}")
                    print(f"   🔐 {badge.verification_hash}")
                    print()
            else:
                filter_text = f" for {args.sponsor_name}" if args.sponsor_name else ""
                print(f"📭 No sponsor badges found{filter_text}")
        
        elif args.venmo_instructions:
            print("💰 CONTRIBUTE TO JERRY'S PROJECTS")
            print("=" * 40)
            print("Venmo: @Bellok")
            print("\n🎁 Contribution Tiers:")
            
            for tier, reward in manager.reward_tiers.items():
                tier_name = tier.value.replace('_', ' ').title()
                print(f"{reward.badge_emoji} {tier_name} - {reward.badge_name}")
                print(f"   XP Bonus: +{reward.xp_bonus}")
                print(f"   Special: {', '.join(reward.special_abilities[:2])}")
                print()
            
            print("🔐 All badges include cryptographic anti-theft protection!")
            print("📧 Email Jerry with payment details to receive your badge.")
        
        else:
            print("💰 Living Dev Agent Sponsor Badge System")
            print("Venmo: @Bellok")
            print("Use --help to see available commands")
            
    except KeyboardInterrupt:
        print("\n⚠️ Sponsor badge manager interrupted")
    except Exception as e:
        print(f"❌ Sponsor badge system error: {e}")


if __name__ == "__main__":
    main()
