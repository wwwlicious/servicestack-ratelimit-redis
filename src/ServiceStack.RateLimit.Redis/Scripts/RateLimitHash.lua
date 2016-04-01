-- This Source Code Form is subject to the terms of the Mozilla Public
-- License, v. 2.0. If a copy of the MPL was not distributed with this 
-- file, You can obtain one at http://mozilla.org/MPL/2.0/.

-- KEYS[1] = consumer. KEYS[2] = request
local payload = cjson.decode(ARGV[1])

-- These are the default values for if a user has no limits
local consumer	= KEYS[1]
local request   = KEYS[2]

-- remove timestamp from limits table (last element) to avoid iterating
local ts        = tonumber(payload.Stamp)

-- Build up return result
local result	= {}
local count		= 1
local durResult = {}
local limitBreached = false
local total		= 0

local function isNewRequest()
	local requestId = payload.RequestId
	if (requestId == nil) then
		return false
	end

	-- The key is kept per 5 minute block
	local requestTtl = 300;
	local requestTs = math.floor(ts / requestTtl)

	-- get key for this bucket and previous (as in Xsec buckets so could have hit cut-off to new bucket)
	local requestKey = 'requestid:' .. requestTs
	local prevKey = 'requestid:' .. (requestTs - 1)

	if (redis.call('SISMEMBER', requestKey, requestId) == 1) then
		-- request exists already, not new
		return false
	elseif (redis.call('SISMEMBER', prevKey, requestId) == 1) then
		return false
	end

	-- key doesn't exist so add it
	redis.call('SADD', requestKey, requestId)

	-- If no TTL (ie we've just created) then set Expire. Set to *1.x ttl to be able to do above check of previous bucket
	if (redis.call('TTL', requestKey) == -1) then 
		redis.call('EXPIRE', requestKey, requestTtl * 1.2) 
	end

	return true
end


-- Iterate and record limits provided for user first as these may be hit when individual limits are okay
local user	= payload['Time']['User']
if user ~= nil then
	-- Before processing user requests, check if this 'requestId' has been processed
	if (isNewRequest()) then

		local userTimes	= user['Limits']
		for i = 1, table.maxn(userTimes) do
			local limit = tonumber(userTimes[i]['Limit'])
			local duration = tonumber(userTimes[i]['Seconds'])

			local key = consumer .. ':' .. duration .. ':' .. math.floor(ts / duration)

			-- Add to a hash key that will be (for mins) request:m:123123 (the 123123 will be block per time)
			if (not limitBreached) then
				total = tonumber(redis.call('INCR', key) or '0')
				redis.call('EXPIRE', key, duration)

				if total > limit then
					limitBreached = true
				end
			else
				total = tonumber(redis.call('GET', key) or '0')
			end

			-- 'durResult is a table of results for this durations. Build that up and set it later
			durResult[count] = {}
			durResult[count]['limit'] = limit
			durResult[count]['Seconds'] = duration
			durResult[count]['current'] = total
			durResult[count]['user'] = true
			count = count + 1
		end
	end
end

--next iterate over all of the resource limits provided 
local requestData = payload['Time']['Request']
if request ~= nil then
	local requestTimes = requestData['Limits']
	for i = 1, table.maxn(requestTimes) do
		local limit = tonumber(requestTimes[i]['Limit'])
		local duration = tonumber(requestTimes[i]['Seconds'])
	
		--only check limits that have been set
		local key = request .. ':' .. duration .. ':' .. math.floor(ts / duration)

		-- Add to a hash key that will be (for mins) request:m:123123 (the 123123 will be block per time)
		if (not limitBreached) then
			total = tonumber(redis.call('HINCRBY', key, consumer, 1) or '0')
			redis.call('EXPIRE', key, duration)

			if total > limit then
				limitBreached = true
			end
		else
			total = tonumber(redis.call('HGET', key, consumer) or '0')
		end
				
		-- 'durResult is a table of results for this durations. Build that up and set it later
		durResult[count] = {}
		durResult[count]['limit'] = limit
		durResult[count]['Seconds'] = duration
		durResult[count]['current'] = total
		durResult[count]['user'] = false
		count = count + 1
	end
end

result['results'] = durResult
result['access'] = not limitBreached
return cjson.encode(result)