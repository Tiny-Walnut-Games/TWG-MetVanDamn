# 🔌 API Contracts v1.0

## Overview

The Living Dev Agent provides versioned API contracts for both bridge integrations and plugin development. This document defines the stable API interfaces available for external systems and extension development.

## Versioning Strategy

### Semantic Versioning
- **MAJOR.MINOR.PATCH** (e.g., 1.0.0)
- **MAJOR**: Breaking changes to API contracts
- **MINOR**: Backward-compatible functionality additions
- **PATCH**: Backward-compatible bug fixes

### API Version Support
- **Current Version**: 1.0.0
- **Supported Versions**: 1.x.x (full support), 0.x.x (deprecated, limited support)
- **Deprecation Policy**: 6-month notice before breaking changes

## Bridge API Contracts

### Core Bridge Interface

#### Event Processing API
```typescript
interface EventProcessor {
  /**
   * Process development events from external systems
   * @param event - The development event to process
   * @returns Promise<ProcessingResult>
   * @since 1.0.0
   */
  processEvent(event: DevelopmentEvent): Promise<ProcessingResult>;
  
  /**
   * Subscribe to event notifications
   * @param eventType - Type of events to subscribe to
   * @param callback - Callback function for event notifications
   * @since 1.0.0
   */
  subscribe(eventType: EventType, callback: EventCallback): void;
  
  /**
   * Unsubscribe from event notifications
   * @param eventType - Type of events to unsubscribe from
   * @param callback - Callback function to remove
   * @since 1.0.0
   */
  unsubscribe(eventType: EventType, callback: EventCallback): void;
}
```

#### TLDL Integration API
```typescript
interface TLDLBridge {
  /**
   * Create a new TLDL entry
   * @param entry - TLDL entry data
   * @returns Promise<TLDLEntry>
   * @since 1.0.0
   */
  createEntry(entry: TLDLEntryData): Promise<TLDLEntry>;
  
  /**
   * Update existing TLDL entry
   * @param id - Entry identifier
   * @param updates - Partial updates to apply
   * @returns Promise<TLDLEntry>
   * @since 1.0.0
   */
  updateEntry(id: string, updates: Partial<TLDLEntryData>): Promise<TLDLEntry>;
  
  /**
   * Query TLDL entries
   * @param query - Search parameters
   * @returns Promise<TLDLEntry[]>
   * @since 1.0.0
   */
  queryEntries(query: TLDLQuery): Promise<TLDLEntry[]>;
}
```

### Unity Bridge API

#### Unity Editor Integration
```csharp
namespace LivingDevAgent.Bridge
{
    /// <summary>
    /// Main bridge interface for Unity Editor integration
    /// </summary>
    public interface IUnityBridge
    {
        /// <summary>
        /// Initialize the bridge connection
        /// </summary>
        /// <param name="config">Bridge configuration</param>
        /// <returns>True if initialization successful</returns>
        bool Initialize(BridgeConfig config);
        
        /// <summary>
        /// Process Unity-specific events
        /// </summary>
        /// <param name="unityEvent">Unity event data</param>
        /// <returns>Processing result</returns>
        Task<ProcessingResult> ProcessUnityEvent(UnityEvent unityEvent);
        
        /// <summary>
        /// Get Unity-specific metrics
        /// </summary>
        /// <returns>Unity development metrics</returns>
        UnityMetrics GetMetrics();
    }
}
```

#### TLDL Wizard Integration
```csharp
namespace LivingDevAgent.Editor
{
    /// <summary>
    /// Unity Editor wizard for TLDL entry creation
    /// </summary>
    public interface ITLDLWizard
    {
        /// <summary>
        /// Show TLDL creation wizard
        /// </summary>
        /// <param name="context">Current development context</param>
        void ShowWizard(DevelopmentContext context);
        
        /// <summary>
        /// Create TLDL entry from wizard data
        /// </summary>
        /// <param name="wizardData">Data from wizard form</param>
        /// <returns>Created TLDL entry</returns>
        TLDLEntry CreateEntry(WizardData wizardData);
    }
}
```

## Plugin API Contracts

### Base Plugin Interface

#### Core Plugin Contract
```python
from abc import ABC, abstractmethod
from typing import Dict, Any, Optional

class BaseLDAPlugin(ABC):
    """Base interface for all LDA plugins"""
    
    @property
    @abstractmethod
    def name(self) -> str:
        """Plugin name identifier"""
        pass
    
    @property
    @abstractmethod
    def version(self) -> str:
        """Plugin version (semantic versioning)"""
        pass
    
    @property
    @abstractmethod
    def api_version(self) -> str:
        """Required LDA API version"""
        pass
    
    @abstractmethod
    def initialize(self, context: 'PluginContext') -> bool:
        """Initialize plugin with LDA context"""
        pass
    
    @abstractmethod
    def process_event(self, event: 'PluginEvent') -> 'PluginResult':
        """Process LDA events"""
        pass
    
    def cleanup(self) -> None:
        """Optional cleanup when plugin is unloaded"""
        pass
```

#### Documentation Plugin Interface
```python
class DocumentationPlugin(BaseLDAPlugin):
    """Specialized interface for documentation plugins"""
    
    @abstractmethod
    def generate_documentation(self, source: str, format: str) -> str:
        """Generate documentation from source code"""
        pass
    
    @abstractmethod
    def validate_documentation(self, doc_path: str) -> 'ValidationResult':
        """Validate documentation quality"""
        pass
```

#### Metrics Plugin Interface
```python
class MetricsPlugin(BaseLDAPlugin):
    """Specialized interface for metrics collection plugins"""
    
    @abstractmethod
    def collect_metrics(self, timeframe: 'TimeFrame') -> Dict[str, Any]:
        """Collect system metrics for specified timeframe"""
        pass
    
    @abstractmethod
    def export_metrics(self, metrics: Dict[str, Any], format: str) -> str:
        """Export metrics in specified format"""
        pass
```

### Plugin Event System

#### Event Types
```python
from enum import Enum

class PluginEventType(Enum):
    """Standard event types for plugin system"""
    DEVELOPMENT_ACTION = "development_action"
    TLDL_CREATED = "tldl_created"
    TLDL_UPDATED = "tldl_updated"
    VALIDATION_COMPLETED = "validation_completed"
    EXPERIMENT_STARTED = "experiment_started"
    EXPERIMENT_COMPLETED = "experiment_completed"
    SECURITY_ALERT = "security_alert"
    PERFORMANCE_METRIC = "performance_metric"
```

#### Event Data Structures
```python
from dataclasses import dataclass
from datetime import datetime
from typing import Optional, Dict, Any

@dataclass
class PluginEvent:
    """Standard event structure for plugin communication"""
    event_type: PluginEventType
    timestamp: datetime
    source: str
    data: Dict[str, Any]
    metadata: Optional[Dict[str, Any]] = None
    
@dataclass
class PluginResult:
    """Standard result structure for plugin operations"""
    success: bool
    message: str
    data: Optional[Dict[str, Any]] = None
    errors: Optional[List[str]] = None
```

## Data Schemas

### TLDL Entry Schema
```yaml
# TLDL Entry v1.0 Schema
title: TLDL Entry
type: object
required:
  - entry_id
  - title
  - author
  - created_date
  - context
  - summary
properties:
  entry_id:
    type: string
    pattern: "^TLDL-\\d{4}-\\d{2}-\\d{2}-.+$"
    description: "Unique identifier for TLDL entry"
  
  title:
    type: string
    maxLength: 100
    description: "Human-readable title"
  
  author:
    type: string
    description: "Entry author"
  
  created_date:
    type: string
    format: date-time
    description: "Entry creation timestamp"
  
  context:
    type: string
    description: "Development context"
  
  summary:
    type: string
    maxLength: 500
    description: "Brief summary of entry contents"
  
  objective:
    type: string
    description: "Primary objective of the development work"
  
  actions_taken:
    type: array
    items:
      type: object
      properties:
        action:
          type: string
        result:
          type: string
        files_modified:
          type: array
          items:
            type: string
  
  key_insights:
    type: array
    items:
      type: string
    description: "Key learning insights"
  
  next_steps:
    type: array
    items:
      type: object
      properties:
        action:
          type: string
        priority:
          type: string
          enum: ["high", "medium", "low"]
        assignee:
          type: string
```

### Event Schema
```yaml
# Development Event v1.0 Schema
title: Development Event
type: object
required:
  - event_id
  - event_type
  - timestamp
  - source
properties:
  event_id:
    type: string
    description: "Unique event identifier"
  
  event_type:
    type: string
    enum:
      - "code_change"
      - "build_completed"
      - "test_executed"
      - "documentation_updated"
      - "security_scan"
    description: "Type of development event"
  
  timestamp:
    type: string
    format: date-time
    description: "Event occurrence timestamp"
  
  source:
    type: string
    description: "Event source system"
  
  payload:
    type: object
    description: "Event-specific data"
  
  metadata:
    type: object
    description: "Additional event metadata"
```

## Configuration Contracts

### Plugin Configuration Schema
```yaml
# Plugin Configuration v1.0 Schema
title: Plugin Configuration
type: object
required:
  - name
  - version
  - api_version
  - entry_point
properties:
  name:
    type: string
    pattern: "^[a-z0-9_-]+$"
    description: "Plugin identifier"
  
  version:
    type: string
    pattern: "^\\d+\\.\\d+\\.\\d+$"
    description: "Plugin version (semantic versioning)"
  
  api_version:
    type: string
    pattern: "^\\d+\\.\\d+\\.\\d+$"
    description: "Required LDA API version"
  
  entry_point:
    type: string
    description: "Plugin entry point module"
  
  dependencies:
    type: array
    items:
      type: string
    description: "Required dependencies"
  
  configuration:
    type: object
    description: "Plugin-specific configuration"
  
  permissions:
    type: array
    items:
      type: string
      enum:
        - "file_system_read"
        - "file_system_write"
        - "network_access"
        - "git_access"
        - "system_metrics"
    description: "Required plugin permissions"
```

## Error Handling Contracts

### Standard Error Responses
```python
from dataclasses import dataclass
from typing import Optional, List

@dataclass
class APIError:
    """Standard API error response"""
    code: str
    message: str
    details: Optional[str] = None
    suggestions: Optional[List[str]] = None
    
    # Standard error codes
    INVALID_INPUT = "INVALID_INPUT"
    AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED"
    AUTHORIZATION_FAILED = "AUTHORIZATION_FAILED"
    RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND"
    INTERNAL_ERROR = "INTERNAL_ERROR"
    SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE"
    RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED"
```

## Backward Compatibility

### Deprecation Policy
1. **Advance Notice**: 6 months minimum before breaking changes
2. **Migration Path**: Clear upgrade instructions provided
3. **Legacy Support**: Previous major version supported for 12 months
4. **Documentation**: Deprecation warnings in all relevant documentation

### Breaking Changes
Breaking changes are introduced only in major version releases and include:
- Removal of deprecated APIs
- Changes to required parameters
- Modifications to return value structures
- Changes to core event schemas

### Non-Breaking Changes
Non-breaking changes can be introduced in minor versions:
- Addition of optional parameters
- New event types
- Additional fields in responses
- New API endpoints

## Security Considerations

### Authentication
- API keys required for external bridge connections
- Token-based authentication for plugin registration
- Role-based access control for sensitive operations

### Data Protection
- All API communications encrypted in transit
- Sensitive data encrypted at rest
- Audit logging for all API access

### Rate Limiting
- API rate limits enforced per client
- Graceful degradation under high load
- Priority queuing for critical operations

## Testing Contracts

### API Testing Requirements
All API implementations must include:
- Unit tests with 90%+ coverage
- Integration tests for external dependencies
- Performance tests meeting SLA requirements
- Security penetration testing

### Plugin Testing Requirements
Plugins must provide:
- Automated test suite
- Example usage documentation
- Performance benchmarks
- Error handling validation

This API contract specification ensures stable, predictable interfaces for all external integrations and plugin development, supporting the v1.0 goal of broader adoption while maintaining system reliability and security.