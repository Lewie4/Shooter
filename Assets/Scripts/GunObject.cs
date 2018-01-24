using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGun", menuName = "Guns/Default")]
public class GunObject : ScriptableObject 
{
	[System.Serializable]
	public class GunLevelData
	{
		public float m_damage;		//Base damage per bullet
		public float m_fireRate;	//RPM
		public float m_accuracy;	//
		public int m_bulletCount;	//Max base clip size
		public float m_reloadTime;	//Seconds to reload clip

		public int m_cost;
	}

	public string m_name;
	public string m_description;

	public Sprite m_artwork;

	public List<GunLevelData> m_gunLevelData;
}
