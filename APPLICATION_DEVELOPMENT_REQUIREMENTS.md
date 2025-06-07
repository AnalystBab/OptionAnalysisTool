# 🚨 CRITICAL: READ THIS FIRST FOR EVERY TASK 🚨
# APPLICATION DEVELOPMENT REQUIREMENTS & TRACKING

> **⚠️ MANDATORY**: Any AI assistant working on this project MUST read this file completely before starting any task.
> This file contains the complete development roadmap, requirements, and current status.

## Project Overview
**Indian Option Market Analysis Tool**
- **Market Focus**: Index Options Only (Indian Market)
- **Primary Purpose**: Circuit Limit Tracking and Analysis
- **Data Source**: Kite Connect API

---

## DEVELOPMENT APPROACH
⚠️ **IMPORTANT**: Before taking any new task, always check this file first to understand current requirements and progress.

---

## CORE REQUIREMENTS

### 1. APPLICATION TIMING & STARTUP
- **Application Start**: 9:00 AM (Pre-open session)
- **Ready State**: 9:15 AM (Market open)
- **Operation**: Continuous data collection during market hours
- **Post-Market**: Data consolidation and storage

### 2. INTRADAY CIRCUIT LIMIT TRACKING
**Real-time Monitoring Requirements:**
- Monitor circuit limit changes for all index option strikes
- Record every circuit limit change with:
  - Date & Time (precise timestamp)
  - Strike Price
  - Contract Details (Symbol, Expiry)
  - Old Circuit Limit Value
  - New Circuit Limit Value
  - Index Level at time of change
  - Other relevant market data

### 3. END-OF-DAY (EOD) DATA PROCESSING
**Post-Market Operations:**
- Collect EOD data from Kite Connect
- **Issue**: Kite EOD data may not include circuit limits
- **Solution**: Merge intraday circuit limit data with EOD data
- Store consolidated data for historical analysis

### 4. HISTORICAL DATA RETRIEVAL
**Query Requirements:**
- View circuit limit changes for any specific date
- Filter by index contract strikes
- Display complete circuit limit change history
- Show all tracker values for selected timeframe

---

## TECHNICAL SPECIFICATIONS

### Data Storage Requirements
- [ ] Real-time circuit limit change logging
- [ ] Historical data storage with efficient querying
- [ ] Date-time indexing for fast retrieval
- [ ] Strike-wise data organization

### API Integration
- [ ] Kite Connect integration for live data
- [ ] Circuit limit monitoring endpoints
- [ ] EOD data fetching
- [ ] Error handling and reconnection logic

### Application Architecture
- [ ] Service-based architecture for continuous operation
- [ ] Data collection service (9 AM start)
- [ ] Circuit limit monitoring service
- [ ] EOD processing service
- [ ] Query/reporting interface

---

## DEVELOPMENT STATUS

### ✅ COMPLETED
- Basic project structure
- Circuit limit documentation
- Database query utilities
- Test implementations

### 🔄 IN PROGRESS
- Circuit limit tracking system
- Database integration

### ⏳ PENDING
- [ ] Application startup at 9 AM automation
- [ ] Real-time circuit limit monitoring
- [ ] Intraday data storage system
- [ ] EOD data merger logic
- [ ] Historical query interface
- [ ] Circuit limit change tracker

---

## NEXT TASKS PRIORITY
1. **High Priority**: Real-time circuit limit monitoring service
2. **High Priority**: Database schema for circuit limit tracking
3. **Medium Priority**: Application startup automation
4. **Medium Priority**: EOD data processing pipeline
5. **Low Priority**: Historical data query interface

---

## NOTES & CONSIDERATIONS
- Indian market hours: 9:15 AM to 3:30 PM
- Pre-open session: 9:00 AM to 9:15 AM
- Circuit limits can change multiple times during the day
- Need reliable data storage for regulatory compliance
- Consider market holidays and trading calendars

---

**Last Updated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Status**: Requirements Documented - Ready for Development

---

## TASK TRACKING
*Before starting any new task, update this section with:*
- Current task description
- Expected completion time
- Dependencies
- Status updates 