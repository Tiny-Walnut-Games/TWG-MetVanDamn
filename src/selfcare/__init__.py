"""
Self-Care & Cognitive Safety Module

The legendary sanctuary for managing high-velocity ideation and cognitive load.
Provides the Idea Vault, Overflow Sluice, Private Journal, and Cognitive Governors
to channel surplus creativity while maintaining developer sanity.

🧙‍♂️ "In the realm of infinite ideas, the wise developer builds dams and channels,
    not to stop the flow, but to harness its power." - Bootstrap Sentinel
"""

__version__ = "1.0.0"
__author__ = "Tiny Walnut Games Living Dev Agent"

# Core components
from .idea_catalog import IdeaCatalog
from .sluice_manager import SluiceManager
from .governors import MeltBudgetGovernor, HumidityGovernor, CognitiveSensitivityFlag
from .journaling import PrivateJournal

# Convenience imports
__all__ = [
    'IdeaCatalog',
    'SluiceManager', 
    'MeltBudgetGovernor',
    'HumidityGovernor',
    'CognitiveSensitivityFlag',
    'PrivateJournal'
]